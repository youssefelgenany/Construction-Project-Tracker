using AutoMapper;
using ConstructionProjectTracker.API.Data;
using ConstructionProjectTracker.API.DTOs.Tasks;
using ConstructionProjectTracker.API.Entities;
using ConstructionProjectTracker.API.Enums;
using ConstructionProjectTracker.API.Helpers;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskStatus = ConstructionProjectTracker.API.Enums.TaskStatus;

namespace ConstructionProjectTracker.API.Services;

public class TaskCompletionReportService : ITaskCompletionReportService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IWebHostEnvironment _environment;
    private readonly ITaskScheduleService _scheduleService;
    private readonly ILogger<TaskCompletionReportService> _logger;

    public TaskCompletionReportService(
        ApplicationDbContext context,
        IMapper mapper,
        IWebHostEnvironment environment,
        ITaskScheduleService scheduleService,
        ILogger<TaskCompletionReportService> logger)
    {
        _context = context;
        _mapper = mapper;
        _environment = environment;
        _scheduleService = scheduleService;
        _logger = logger;
    }

    public async Task<TaskCompletionReportDto> UploadAsync(int taskId, IFormFile file, int userId)
    {
        var task = await _context.Tasks
            .Include(t => t.CompletionReport)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task is null)
            throw new InvalidOperationException("Task does not exist.");

        if (!await IsAssignedEngineerAsync(task, userId))
            throw new UnauthorizedAccessException("You do not have permission to submit a completion report for this task.");

        if (task.Status is TaskStatus.PendingReview or TaskStatus.Completed)
            throw new InvalidOperationException("A completion report cannot be submitted for this task in its current state.");

        if (task.Status == TaskStatus.Blocked)
            throw new InvalidOperationException(
                "This task is blocked until prerequisite tasks are completed and approved.");

        if (task.Status != TaskStatus.InProgress || task.CompletionPercentage < 90)
            throw new InvalidOperationException(
                "Task must be In Progress at 90% or higher before submitting a completion report.");

        ValidateFile(file);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var storedFileName = TaskCompletionReportFileRules.BuildStoredFileName(extension);
        var relativeDirectory = TaskCompletionReportFileRules.GetRelativeDirectory(taskId);
        var relativeFilePath = $"{relativeDirectory}/{storedFileName}".Replace('\\', '/');

        var physicalDirectory = Path.Combine(
            _environment.WebRootPath,
            relativeDirectory.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(physicalDirectory);

        if (task.CompletionReport is not null)
            await DeletePhysicalFileAsync(task.CompletionReport.RelativeFilePath);

        var physicalFilePath = Path.Combine(physicalDirectory, storedFileName);

        await using (var stream = new FileStream(physicalFilePath, FileMode.CreateNew))
        {
            await file.CopyToAsync(stream);
        }

        if (task.CompletionReport is null)
        {
            task.CompletionReport = new TaskCompletionReport
            {
                TaskId = taskId,
                UploadedByUserId = userId
            };
            _context.TaskCompletionReports.Add(task.CompletionReport);
        }

        task.CompletionReport.OriginalFileName = file.FileName;
        task.CompletionReport.StoredFileName = storedFileName;
        task.CompletionReport.Extension = extension;
        task.CompletionReport.ContentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;
        task.CompletionReport.FileSize = file.Length;
        task.CompletionReport.RelativeFilePath = relativeFilePath;
        task.CompletionReport.UploadedAt = DateTime.UtcNow;
        task.CompletionReport.UploadedByUserId = userId;
        task.CompletionReport.RejectionComment = null;
        task.CompletionReport.ReviewedByUserId = null;
        task.CompletionReport.ReviewedAt = null;

        task.CompletionPercentage = 100;
        task.Status = TaskStatus.PendingReview;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await RecalculateProjectProgressAsync(task.ProjectId);

        _logger.LogInformation(
            "Completion report uploaded: TaskId={TaskId}, UserId={UserId}",
            taskId, userId);

        return await MapReportDtoAsync(taskId);
    }

    public async Task<FileStreamResult?> DownloadAsync(int taskId, int userId, bool isAdmin)
    {
        var task = await _context.Tasks
            .AsNoTracking()
            .Include(t => t.CompletionReport)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task?.CompletionReport is null)
            return null;

        if (!isAdmin && !await IsAssignedEngineerAsync(task, userId))
            throw new UnauthorizedAccessException("You do not have permission to download this completion report.");

        var report = task.CompletionReport;
        var physicalFilePath = GetPhysicalPath(report.RelativeFilePath);

        if (!File.Exists(physicalFilePath))
            throw new FileNotFoundException("The completion report file could not be found.");

        var stream = new FileStream(physicalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        _logger.LogInformation(
            "Completion report downloaded: TaskId={TaskId}, UserId={UserId}",
            taskId, userId);

        return new FileStreamResult(stream, report.ContentType)
        {
            FileDownloadName = report.OriginalFileName
        };
    }

    public async Task<TaskResponseDto> ApproveAsync(int taskId, int userId)
    {
        var task = await _context.Tasks
            .Include(t => t.CompletionReport!)
                .ThenInclude(r => r.ApprovalHistory)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task is null)
            throw new InvalidOperationException("Task does not exist.");

        if (task.Status != TaskStatus.PendingReview || task.CompletionReport is null)
            throw new InvalidOperationException("This task does not have a completion report pending review.");

        task.Status = TaskStatus.Completed;
        task.CompletionPercentage = 100;
        task.UpdatedAt = DateTime.UtcNow;
        task.CompletionReport.RejectionComment = null;
        task.CompletionReport.ReviewedByUserId = userId;
        task.CompletionReport.ReviewedAt = DateTime.UtcNow;
        task.CompletionReport.ApprovalHistory.Add(new TaskCompletionApprovalHistory
        {
            Action = "Approved",
            ReviewedByUserId = userId,
            ReviewedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        await RecalculateProjectProgressAsync(task.ProjectId);
        await _scheduleService.RefreshProjectBlockingStatesAsync(task.ProjectId);

        _logger.LogInformation(
            "Completion report approved: TaskId={TaskId}, UserId={UserId}",
            taskId, userId);

        return (await MapTaskResponseAsync(taskId))!;
    }

    public async Task<TaskResponseDto> RejectAsync(int taskId, RejectCompletionReportDto dto, int userId)
    {
        var task = await _context.Tasks
            .Include(t => t.CompletionReport!)
                .ThenInclude(r => r.ApprovalHistory)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task is null)
            throw new InvalidOperationException("Task does not exist.");

        if (task.Status != TaskStatus.PendingReview || task.CompletionReport is null)
            throw new InvalidOperationException("This task does not have a completion report pending review.");

        var reason = dto.Comment.Trim();
        var reviewedAt = DateTime.UtcNow;

        task.Status = TaskStatus.InProgress;
        // Keep progress at 100% after rejection — engineer must re-submit a report for approval.
        task.CompletionPercentage = 100;
        task.UpdatedAt = reviewedAt;
        task.CompletionReport.RejectionComment = reason;
        task.CompletionReport.ReviewedByUserId = userId;
        task.CompletionReport.ReviewedAt = reviewedAt;
        task.CompletionReport.ApprovalHistory.Add(new TaskCompletionApprovalHistory
        {
            Action = "Rejected",
            ReviewedByUserId = userId,
            ReviewedAt = reviewedAt,
            RejectionReason = reason
        });

        await _context.SaveChangesAsync();
        await RecalculateProjectProgressAsync(task.ProjectId);

        _logger.LogInformation(
            "Completion report rejected: TaskId={TaskId}, UserId={UserId}",
            taskId, userId);

        return (await MapTaskResponseAsync(taskId))!;
    }

    public Task DeletePhysicalFileAsync(string relativeFilePath)
    {
        var physicalFilePath = GetPhysicalPath(relativeFilePath);
        if (File.Exists(physicalFilePath))
            File.Delete(physicalFilePath);

        return Task.CompletedTask;
    }

    private static void ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
            throw new InvalidOperationException("File is required.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!TaskCompletionReportFileRules.IsAllowedExtension(extension))
            throw new InvalidOperationException("File type is not allowed.");

        if (file.Length > TaskCompletionReportFileRules.MaxFileSizeBytes)
            throw new InvalidOperationException("File size must not exceed 20 MB.");
    }

    private async Task<bool> IsAssignedEngineerAsync(TaskItem task, int userId)
    {
        if (!task.AssignedEngineerId.HasValue)
            return false;

        var engineerId = await _context.Engineers
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync();

        return engineerId.HasValue && task.AssignedEngineerId == engineerId.Value;
    }

    private async Task<TaskCompletionReportDto> MapReportDtoAsync(int taskId)
    {
        var report = await _context.TaskCompletionReports
            .AsNoTracking()
            .Include(r => r.UploadedByUser)
            .Include(r => r.ReviewedByUser)
            .Include(r => r.ApprovalHistory)
                .ThenInclude(h => h.ReviewedByUser)
            .FirstAsync(r => r.TaskId == taskId);

        return MapReportDto(report, await GetTaskStatusAsync(taskId));
    }

    private async Task<TaskStatus> GetTaskStatusAsync(int taskId) =>
        await _context.Tasks.AsNoTracking()
            .Where(t => t.Id == taskId)
            .Select(t => t.Status)
            .FirstAsync();

    private TaskCompletionReportDto MapReportDto(TaskCompletionReport report, TaskStatus? taskStatus = null)
    {
        var isRejected = !string.IsNullOrWhiteSpace(report.RejectionComment);
        var isApproved = report.ReviewedAt.HasValue && !isRejected && taskStatus == TaskStatus.Completed;
        var isPending = taskStatus == TaskStatus.PendingReview || !report.ReviewedAt.HasValue;

        string approvalStatus;
        if (isPending)
            approvalStatus = "Pending Review";
        else if (isRejected)
            approvalStatus = "Rejected";
        else if (isApproved)
            approvalStatus = "Approved";
        else
            approvalStatus = report.ReviewedAt.HasValue ? "Reviewed" : "Pending Review";

        return new TaskCompletionReportDto
        {
            Id = report.Id,
            TaskId = report.TaskId,
            OriginalFileName = report.OriginalFileName,
            Extension = report.Extension,
            ContentType = report.ContentType,
            FileSize = report.FileSize,
            UploadedAt = report.UploadedAt,
            UploadedBy = report.UploadedByUser.FullName,
            ApprovalStatus = approvalStatus,
            ReviewedBy = report.ReviewedByUser?.FullName,
            ReviewedAt = report.ReviewedAt,
            RejectedBy = isRejected ? report.ReviewedByUser?.FullName : null,
            RejectedAt = isRejected ? report.ReviewedAt : null,
            RejectionReason = report.RejectionComment,
            RejectionComment = report.RejectionComment,
            ApprovalHistory = report.ApprovalHistory
                .OrderByDescending(h => h.ReviewedAt)
                .Select(h => new TaskCompletionApprovalHistoryDto
                {
                    Id = h.Id,
                    Action = h.Action,
                    ReviewedBy = h.ReviewedByUser?.FullName ?? string.Empty,
                    ReviewedAt = h.ReviewedAt,
                    RejectionReason = h.RejectionReason
                })
                .ToList()
        };
    }

    private async Task<TaskResponseDto?> MapTaskResponseAsync(int taskId)
    {
        var task = await _context.Tasks
            .AsNoTracking()
            .Include(t => t.Project)
            .Include(t => t.AssignedEngineer!)
                .ThenInclude(e => e.User)
            .Include(t => t.CompletionReport!)
                .ThenInclude(r => r.UploadedByUser)
            .Include(t => t.CompletionReport!)
                .ThenInclude(r => r.ReviewedByUser)
            .Include(t => t.CompletionReport!)
                .ThenInclude(r => r.ApprovalHistory)
                    .ThenInclude(h => h.ReviewedByUser)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task is null)
            return null;

        var dto = _mapper.Map<TaskResponseDto>(task);
        dto.CompletionReport = task.CompletionReport is null
            ? null
            : MapReportDto(task.CompletionReport, task.Status);

        return dto;
    }

    private async Task RecalculateProjectProgressAsync(int projectId)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project is null)
            return;

        var tasks = await _context.Tasks
            .Where(t => t.ProjectId == projectId)
            .ToListAsync();

        if (tasks.Count == 0)
        {
            project.ProgressPercentage = 0;
            project.Status = ProjectStatus.NotStarted;
        }
        else
        {
            project.ProgressPercentage = (int)Math.Round(
                tasks.Average(t => t.CompletionPercentage),
                MidpointRounding.AwayFromZero);

            project.Status = tasks.All(t => t.Status == TaskStatus.Completed)
                ? ProjectStatus.Completed
                : project.ProgressPercentage > 0
                    ? ProjectStatus.InProgress
                    : ProjectStatus.NotStarted;
        }

        await _context.SaveChangesAsync();
    }

    private string GetPhysicalPath(string relativeFilePath) =>
        Path.Combine(_environment.WebRootPath, relativeFilePath.Replace('/', Path.DirectorySeparatorChar));
}
