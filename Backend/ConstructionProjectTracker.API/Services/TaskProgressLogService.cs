using ConstructionProjectTracker.API.Data;
using ConstructionProjectTracker.API.DTOs.Tasks;
using ConstructionProjectTracker.API.Entities;
using ConstructionProjectTracker.API.Enums;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.EntityFrameworkCore;
using TaskStatus = ConstructionProjectTracker.API.Enums.TaskStatus;

namespace ConstructionProjectTracker.API.Services;

public class TaskProgressLogService : ITaskProgressLogService
{
    private const int MaxManualProgress = 90;
    private const int MinDescriptionLength = 10;

    private readonly ApplicationDbContext _context;
    private readonly ILogger<TaskProgressLogService> _logger;

    public TaskProgressLogService(
        ApplicationDbContext context,
        ILogger<TaskProgressLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TaskProgressLogDto>> GetByTaskIdAsync(int taskId, int userId, bool isAdmin)
    {
        if (!await CanAccessTaskAsync(taskId, userId, isAdmin))
            throw new UnauthorizedAccessException("You do not have permission to view this task's progress history.");

        var logs = await _context.TaskProgressLogs
            .AsNoTracking()
            .Include(l => l.Engineer)
                .ThenInclude(e => e.User)
            .Where(l => l.TaskId == taskId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        return logs.Select(MapToDto).ToList();
    }

    public async Task<TaskProgressLogDto> CreateAsync(
        int taskId,
        CreateTaskProgressLogDto dto,
        int userId,
        bool isAdmin)
    {
        ValidateCreateDto(dto);

        var task = await _context.Tasks.FindAsync(taskId);
        if (task is null)
            throw new InvalidOperationException("Task does not exist.");

        if (!await CanAccessTaskAsync(task, userId, isAdmin))
            throw new UnauthorizedAccessException("You do not have permission to update this task's progress.");

        if (task.Status is TaskStatus.PendingReview or TaskStatus.Completed)
            throw new InvalidOperationException("Progress cannot be updated in the task's current state.");

        if (task.Status == TaskStatus.Blocked)
            throw new InvalidOperationException(
                "This task is blocked until prerequisite tasks are completed and approved.");

        var previousProgress = task.CompletionPercentage;

        if (dto.NewProgress <= previousProgress)
            throw new InvalidOperationException("Progress must be greater than the current progress.");

        if (dto.NewProgress > MaxManualProgress)
            throw new InvalidOperationException($"Manual progress cannot exceed {MaxManualProgress}%.");

        var engineerId = await ResolveEngineerIdAsync(task, userId, isAdmin);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var log = new TaskProgressLog
            {
                TaskId = taskId,
                EngineerId = engineerId,
                PreviousProgress = previousProgress,
                NewProgress = dto.NewProgress,
                Description = dto.Description.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.TaskProgressLogs.Add(log);

            task.CompletionPercentage = dto.NewProgress;
            task.Status = DeriveStatusFromProgress(dto.NewProgress);
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await RecalculateProjectProgressAsync(task.ProjectId);
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Task progress log created: TaskId={TaskId}, EngineerId={EngineerId}, {Previous}% -> {New}%",
                taskId, engineerId, previousProgress, dto.NewProgress);

            var created = await _context.TaskProgressLogs
                .AsNoTracking()
                .Include(l => l.Engineer)
                    .ThenInclude(e => e.User)
                .FirstAsync(l => l.Id == log.Id);

            return MapToDto(created);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static void ValidateCreateDto(CreateTaskProgressLogDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Description))
            throw new InvalidOperationException("A description is required when updating progress.");

        if (dto.Description.Trim().Length < MinDescriptionLength)
            throw new InvalidOperationException($"Description must be at least {MinDescriptionLength} characters.");
    }

    private async Task<int> ResolveEngineerIdAsync(TaskItem task, int userId, bool isAdmin)
    {
        if (!isAdmin)
        {
            var engineerId = await GetEngineerIdForUserAsync(userId);
            if (!engineerId.HasValue || task.AssignedEngineerId != engineerId.Value)
                throw new UnauthorizedAccessException("You can only update progress on tasks assigned to you.");

            return engineerId.Value;
        }

        if (!task.AssignedEngineerId.HasValue)
            throw new InvalidOperationException("Task must have an assigned engineer to record progress.");

        return task.AssignedEngineerId.Value;
    }

    private async Task<bool> CanAccessTaskAsync(int taskId, int userId, bool isAdmin)
    {
        var task = await _context.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
        return task is not null && await CanAccessTaskAsync(task, userId, isAdmin);
    }

    private async Task<bool> CanAccessTaskAsync(TaskItem task, int userId, bool isAdmin)
    {
        if (isAdmin)
            return true;

        var engineerId = await GetEngineerIdForUserAsync(userId);
        return engineerId.HasValue && task.AssignedEngineerId == engineerId.Value;
    }

    private async Task<int?> GetEngineerIdForUserAsync(int userId) =>
        await _context.Engineers
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync();

    private static TaskStatus DeriveStatusFromProgress(int progress) =>
        progress switch
        {
            0 => TaskStatus.NotStarted,
            _ => TaskStatus.InProgress
        };

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

    private static TaskProgressLogDto MapToDto(TaskProgressLog log) =>
        new()
        {
            Id = log.Id,
            TaskId = log.TaskId,
            EngineerId = log.EngineerId,
            EngineerName = log.Engineer.User.FullName,
            PreviousProgress = log.PreviousProgress,
            NewProgress = log.NewProgress,
            Description = log.Description,
            CreatedAt = log.CreatedAt
        };
}
