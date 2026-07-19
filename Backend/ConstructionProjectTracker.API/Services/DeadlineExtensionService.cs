using ConstructionProjectTracker.API.Data;
using ConstructionProjectTracker.API.DTOs.DeadlineExtensions;
using ConstructionProjectTracker.API.Entities;
using ConstructionProjectTracker.API.Enums;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.EntityFrameworkCore;
using TaskStatus = ConstructionProjectTracker.API.Enums.TaskStatus;

namespace ConstructionProjectTracker.API.Services;

public class DeadlineExtensionService : IDeadlineExtensionService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ITaskDeadlineCascadeService _cascadeService;

    public DeadlineExtensionService(
        ApplicationDbContext context,
        INotificationService notificationService,
        ITaskDeadlineCascadeService cascadeService)
    {
        _context = context;
        _notificationService = notificationService;
        _cascadeService = cascadeService;
    }

    public async Task<DeadlineExtensionRequestDto> CreateTaskRequestAsync(
        int taskId,
        CreateTaskDeadlineExtensionRequestDto dto,
        int userId)
    {
        var task = await _context.Tasks
            .Include(t => t.Project)
            .Include(t => t.AssignedEngineer)!.ThenInclude(e => e!.User)
            .FirstOrDefaultAsync(t => t.Id == taskId)
            ?? throw new KeyNotFoundException("Task not found.");

        await EnsureAssignedEngineerAsync(task, userId);

        if (task.Status is TaskStatus.Completed)
            throw new InvalidOperationException("Completed tasks cannot receive deadline extension requests.");

        var requestedDate = dto.RequestedDueDate.Date;
        var currentDue = task.DueDate.Date;
        var projectEnd = task.Project.EndDate.Date;

        if (requestedDate <= currentDue)
            throw new InvalidOperationException("Requested due date must be after the current task due date.");

        if (requestedDate > projectEnd)
            throw new InvalidOperationException("Requested due date must remain within the project's end date.");

        if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Trim().Length < 20)
            throw new InvalidOperationException("Reason must be at least 20 characters.");

        var hasPending = await _context.TaskDeadlineExtensionRequests
            .AnyAsync(r => r.TaskId == taskId && r.Status == ExtensionRequestStatus.Pending);
        if (hasPending)
            throw new InvalidOperationException("There is already a pending deadline extension request for this task.");

        var requester = await GetUserAsync(userId);
        var request = new TaskDeadlineExtensionRequest
        {
            TaskId = taskId,
            RequestedByUserId = userId,
            CurrentDueDate = task.DueDate,
            RequestedDueDate = requestedDate,
            Reason = dto.Reason.Trim(),
            Status = ExtensionRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.TaskDeadlineExtensionRequests.Add(request);
        await _context.SaveChangesAsync();

        var dateLabel = FormatDate(requestedDate);
        var message =
            $"Engineer {requester.FullName} requested extending task \"{task.Title}\" until {dateLabel}.";

        await _notificationService.NotifyUsersAsync(
            await GetAdminUserIdsAsync(),
            NotificationType.DeadlineExtensionRequest,
            "Task deadline extension requested",
            message,
            "TaskDeadlineExtensionRequest",
            request.Id);

        return await MapTaskRequestAsync(request.Id);
    }

    public async Task<DeadlineExtensionRequestDto> CreateProjectRequestAsync(
        int projectId,
        CreateProjectDeadlineExtensionRequestDto dto,
        int userId)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectAssignments)
            .FirstOrDefaultAsync(p => p.Id == projectId)
            ?? throw new KeyNotFoundException("Project not found.");

        await EnsureProjectAssignedEngineerAsync(projectId, userId);

        if (project.Status == ProjectStatus.Completed)
            throw new InvalidOperationException("Completed projects cannot receive deadline extension requests.");

        var requestedDate = dto.RequestedEndDate.Date;
        var currentEnd = project.EndDate.Date;

        if (requestedDate <= currentEnd)
            throw new InvalidOperationException("Requested end date must be after the current project end date.");

        if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Trim().Length < 20)
            throw new InvalidOperationException("Reason must be at least 20 characters.");

        var hasPending = await _context.ProjectDeadlineExtensionRequests
            .AnyAsync(r => r.ProjectId == projectId && r.Status == ExtensionRequestStatus.Pending);
        if (hasPending)
            throw new InvalidOperationException("There is already a pending deadline extension request for this project.");

        var requester = await GetUserAsync(userId);
        var request = new ProjectDeadlineExtensionRequest
        {
            ProjectId = projectId,
            RequestedByUserId = userId,
            CurrentEndDate = project.EndDate,
            RequestedEndDate = requestedDate,
            Reason = dto.Reason.Trim(),
            Status = ExtensionRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProjectDeadlineExtensionRequests.Add(request);
        await _context.SaveChangesAsync();

        var dateLabel = FormatDate(requestedDate);
        var message =
            $"Engineer {requester.FullName} requested extending project \"{project.Name}\" until {dateLabel}.";

        await _notificationService.NotifyUsersAsync(
            await GetAdminUserIdsAsync(),
            NotificationType.DeadlineExtensionRequest,
            "Project deadline extension requested",
            message,
            "ProjectDeadlineExtensionRequest",
            request.Id);

        return await MapProjectRequestAsync(request.Id);
    }

    public async Task<DeadlineExtensionRequestDto?> GetLatestTaskRequestAsync(int taskId, int userId, bool isAdmin)
    {
        var task = await _context.Tasks
            .Include(t => t.AssignedEngineer)
            .FirstOrDefaultAsync(t => t.Id == taskId)
            ?? throw new KeyNotFoundException("Task not found.");

        if (!isAdmin)
            await EnsureAssignedEngineerAsync(task, userId);

        var latestId = await _context.TaskDeadlineExtensionRequests
            .Where(r => r.TaskId == taskId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync();

        return latestId is null ? null : await MapTaskRequestAsync(latestId.Value);
    }

    public async Task<DeadlineExtensionRequestDto?> GetLatestProjectRequestAsync(int projectId, int userId, bool isAdmin)
    {
        _ = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId)
            ?? throw new KeyNotFoundException("Project not found.");

        if (!isAdmin)
            await EnsureProjectAssignedEngineerAsync(projectId, userId);

        var latestId = await _context.ProjectDeadlineExtensionRequests
            .Where(r => r.ProjectId == projectId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync();

        return latestId is null ? null : await MapProjectRequestAsync(latestId.Value);
    }

    public async Task<IReadOnlyList<DeadlineExtensionRequestDto>> GetAdminRequestsAsync(ExtensionRequestStatus? status)
    {
        var taskQuery = _context.TaskDeadlineExtensionRequests
            .AsNoTracking()
            .Include(r => r.Task).ThenInclude(t => t.Project)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ReviewedByUser)
            .AsQueryable();

        var projectQuery = _context.ProjectDeadlineExtensionRequests
            .AsNoTracking()
            .Include(r => r.Project)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ReviewedByUser)
            .AsQueryable();

        if (status.HasValue)
        {
            taskQuery = taskQuery.Where(r => r.Status == status.Value);
            projectQuery = projectQuery.Where(r => r.Status == status.Value);
        }

        var tasks = await taskQuery.ToListAsync();
        var projects = await projectQuery.ToListAsync();

        var results = tasks.Select(MapTaskRequestEntity)
            .Concat(projects.Select(MapProjectRequestEntity))
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        return results;
    }

    public async Task<DeadlineExtensionRequestDto> ApproveTaskRequestAsync(
        int requestId,
        ReviewDeadlineExtensionDto dto,
        int adminUserId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var request = await _context.TaskDeadlineExtensionRequests
                .Include(r => r.Task).ThenInclude(t => t.Project)
                .Include(r => r.Task).ThenInclude(t => t.AssignedEngineer)!.ThenInclude(e => e!.User)
                .Include(r => r.RequestedByUser)
                .FirstOrDefaultAsync(r => r.Id == requestId)
                ?? throw new KeyNotFoundException("Task deadline extension request not found.");

            if (request.Status != ExtensionRequestStatus.Pending)
                throw new InvalidOperationException("Only pending requests can be approved.");

            var admin = await GetUserAsync(adminUserId);
            var previousDue = request.Task.DueDate;
            var newDue = request.RequestedDueDate.Date;
            var applyReason = $"Approved extension request: {request.Reason}";

            // Stage task + dependency + project updates on tracked entities (no save yet).
            var staged = await _cascadeService.StageApplyAsync(
                request.TaskId,
                new ApplyTaskDeadlineExtensionDto
                {
                    NewDueDate = newDue,
                    Reason = applyReason,
                    ConfirmProjectExtension = dto.ConfirmProjectExtension
                },
                adminUserId);

            request.Status = ExtensionRequestStatus.Approved;
            request.AdminComment = NormalizeComment(dto.AdminComment);
            request.ReviewedByUserId = adminUserId;
            request.ReviewedAt = DateTime.UtcNow;

            AddAudit(
                adminUserId,
                "TaskDeadlineExtensionApproved",
                $"Administrator {admin.FullName} approved task deadline extension from {FormatDate(previousDue)} to {FormatDate(newDue)}.",
                "Task",
                request.TaskId);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _cascadeService.NotifyStagedApplyAsync(staged);

            var message =
                $"Administrator {admin.FullName} approved your extension request for task \"{request.Task.Title}\".";
            if (!string.IsNullOrWhiteSpace(request.AdminComment))
                message += $"\n\nReason:\n{request.AdminComment}";

            await _notificationService.NotifyUsersAsync(
                [request.RequestedByUserId],
                NotificationType.DeadlineExtensionApproved,
                "Deadline extension approved",
                message,
                "Task",
                request.TaskId);

            return await MapTaskRequestAsync(request.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<DeadlineExtensionRequestDto> RejectTaskRequestAsync(
        int requestId,
        ReviewDeadlineExtensionDto dto,
        int adminUserId)
    {
        var request = await _context.TaskDeadlineExtensionRequests
            .Include(r => r.Task)
            .Include(r => r.RequestedByUser)
            .FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new KeyNotFoundException("Task deadline extension request not found.");

        if (request.Status != ExtensionRequestStatus.Pending)
            throw new InvalidOperationException("Only pending requests can be rejected.");

        if (string.IsNullOrWhiteSpace(dto.AdminComment))
            throw new InvalidOperationException("Admin comment is required when rejecting a request.");

        var admin = await GetUserAsync(adminUserId);
        request.Status = ExtensionRequestStatus.Rejected;
        request.AdminComment = dto.AdminComment.Trim();
        request.ReviewedByUserId = adminUserId;
        request.ReviewedAt = DateTime.UtcNow;

        AddAudit(
            adminUserId,
            "TaskDeadlineExtensionRejected",
            $"Administrator {admin.FullName} rejected task deadline extension from {FormatDate(request.CurrentDueDate)} to {FormatDate(request.RequestedDueDate)}.",
            "Task",
            request.TaskId);

        await _context.SaveChangesAsync();

        var message =
            $"Administrator {admin.FullName} rejected your extension request for task \"{request.Task.Title}\".\n\nReason:\n{request.AdminComment}";

        await _notificationService.NotifyUsersAsync(
            [request.RequestedByUserId],
            NotificationType.DeadlineExtensionRejected,
            "Deadline extension rejected",
            message,
            "Task",
            request.TaskId);

        return await MapTaskRequestAsync(request.Id);
    }

    public async Task<DeadlineExtensionRequestDto> ApproveProjectRequestAsync(
        int requestId,
        ReviewDeadlineExtensionDto dto,
        int adminUserId)
    {
        var request = await _context.ProjectDeadlineExtensionRequests
            .Include(r => r.Project)
            .Include(r => r.RequestedByUser)
            .FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new KeyNotFoundException("Project deadline extension request not found.");

        if (request.Status != ExtensionRequestStatus.Pending)
            throw new InvalidOperationException("Only pending requests can be approved.");

        var admin = await GetUserAsync(adminUserId);
        var previousEnd = request.Project.EndDate;
        var newEnd = request.RequestedEndDate.Date;

        request.Project.EndDate = newEnd;
        request.Status = ExtensionRequestStatus.Approved;
        request.AdminComment = NormalizeComment(dto.AdminComment);
        request.ReviewedByUserId = adminUserId;
        request.ReviewedAt = DateTime.UtcNow;

        _context.ProjectDeadlineHistories.Add(new ProjectDeadlineHistory
        {
            ProjectId = request.ProjectId,
            PreviousEndDate = previousEnd,
            NewEndDate = newEnd,
            Reason = $"Approved extension request: {request.Reason}",
            ChangedByUserId = adminUserId,
            ChangedAt = DateTime.UtcNow
        });

        AddAudit(
            adminUserId,
            "ProjectDeadlineExtensionApproved",
            $"Administrator {admin.FullName} approved project deadline extension from {FormatDate(previousEnd)} to {FormatDate(newEnd)}.",
            "Project",
            request.ProjectId);

        await _context.SaveChangesAsync();

        var message =
            $"Administrator {admin.FullName} approved your extension request for project \"{request.Project.Name}\".";
        if (!string.IsNullOrWhiteSpace(request.AdminComment))
            message += $"\n\nReason:\n{request.AdminComment}";

        await _notificationService.NotifyUsersAsync(
            [request.RequestedByUserId],
            NotificationType.DeadlineExtensionApproved,
            "Deadline extension approved",
            message,
            "Project",
            request.ProjectId);

        return await MapProjectRequestAsync(request.Id);
    }

    public async Task<DeadlineExtensionRequestDto> RejectProjectRequestAsync(
        int requestId,
        ReviewDeadlineExtensionDto dto,
        int adminUserId)
    {
        var request = await _context.ProjectDeadlineExtensionRequests
            .Include(r => r.Project)
            .Include(r => r.RequestedByUser)
            .FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new KeyNotFoundException("Project deadline extension request not found.");

        if (request.Status != ExtensionRequestStatus.Pending)
            throw new InvalidOperationException("Only pending requests can be rejected.");

        if (string.IsNullOrWhiteSpace(dto.AdminComment))
            throw new InvalidOperationException("Admin comment is required when rejecting a request.");

        var admin = await GetUserAsync(adminUserId);
        request.Status = ExtensionRequestStatus.Rejected;
        request.AdminComment = dto.AdminComment.Trim();
        request.ReviewedByUserId = adminUserId;
        request.ReviewedAt = DateTime.UtcNow;

        AddAudit(
            adminUserId,
            "ProjectDeadlineExtensionRejected",
            $"Administrator {admin.FullName} rejected project deadline extension from {FormatDate(request.CurrentEndDate)} to {FormatDate(request.RequestedEndDate)}.",
            "Project",
            request.ProjectId);

        await _context.SaveChangesAsync();

        var message =
            $"Administrator {admin.FullName} rejected your extension request for project \"{request.Project.Name}\".\n\nReason:\n{request.AdminComment}";

        await _notificationService.NotifyUsersAsync(
            [request.RequestedByUserId],
            NotificationType.DeadlineExtensionRejected,
            "Deadline extension rejected",
            message,
            "Project",
            request.ProjectId);

        return await MapProjectRequestAsync(request.Id);
    }

    public async Task ExtendTaskDeadlineAsync(int taskId, AdminExtendTaskDeadlineDto dto, int adminUserId)
    {
        var task = await _context.Tasks
            .Include(t => t.Project)
            .Include(t => t.AssignedEngineer)!.ThenInclude(e => e!.User)
            .FirstOrDefaultAsync(t => t.Id == taskId)
            ?? throw new KeyNotFoundException("Task not found.");

        var newDue = dto.NewDueDate.Date;
        var previousDue = task.DueDate;

        if (newDue <= previousDue.Date)
            throw new InvalidOperationException("New due date must be after the current due date.");

        if (newDue > task.Project.EndDate.Date)
            throw new InvalidOperationException("New due date cannot exceed the project's end date.");

        if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Trim().Length < 10)
            throw new InvalidOperationException("Reason for extension is required.");

        var admin = await GetUserAsync(adminUserId);
        var reason = dto.Reason.Trim();

        task.DueDate = newDue;
        task.UpdatedAt = DateTime.UtcNow;

        _context.TaskDeadlineHistories.Add(new TaskDeadlineHistory
        {
            TaskId = taskId,
            PreviousDueDate = previousDue,
            NewDueDate = newDue,
            Reason = reason,
            ChangedByUserId = adminUserId,
            ChangedAt = DateTime.UtcNow
        });

        AddAudit(
            adminUserId,
            "TaskDeadlineExtended",
            $"Administrator {admin.FullName} extended the deadline for task \"{task.Title}\" from {FormatDate(previousDue)} to {FormatDate(newDue)}.",
            "Task",
            taskId);

        await _context.SaveChangesAsync();

        if (task.AssignedEngineer?.UserId is int engineerUserId)
        {
            var message =
                $"Administrator {admin.FullName} extended the deadline for task \"{task.Title}\" from {FormatDate(previousDue)} to {FormatDate(newDue)}.\n\nReason:\n{reason}";

            await _notificationService.NotifyUsersAsync(
                [engineerUserId],
                NotificationType.DeadlineExtended,
                "Task deadline extended",
                message,
                "Task",
                taskId);
        }
    }

    public async Task ExtendProjectDeadlineAsync(int projectId, AdminExtendProjectDeadlineDto dto, int adminUserId)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectAssignments)
            .ThenInclude(a => a.Engineer)
            .FirstOrDefaultAsync(p => p.Id == projectId)
            ?? throw new KeyNotFoundException("Project not found.");

        var newEnd = dto.NewEndDate.Date;
        var previousEnd = project.EndDate;

        if (newEnd <= previousEnd.Date)
            throw new InvalidOperationException("New end date must be after the current project end date.");

        if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Trim().Length < 10)
            throw new InvalidOperationException("Reason for extension is required.");

        var admin = await GetUserAsync(adminUserId);
        var reason = dto.Reason.Trim();

        project.EndDate = newEnd;

        _context.ProjectDeadlineHistories.Add(new ProjectDeadlineHistory
        {
            ProjectId = projectId,
            PreviousEndDate = previousEnd,
            NewEndDate = newEnd,
            Reason = reason,
            ChangedByUserId = adminUserId,
            ChangedAt = DateTime.UtcNow
        });

        AddAudit(
            adminUserId,
            "ProjectDeadlineExtended",
            $"Administrator {admin.FullName} extended the deadline for project \"{project.Name}\" from {FormatDate(previousEnd)} to {FormatDate(newEnd)}.",
            "Project",
            projectId);

        await _context.SaveChangesAsync();

        var engineerUserIds = project.ProjectAssignments
            .Select(a => a.Engineer.UserId)
            .Distinct()
            .ToList();

        if (engineerUserIds.Count > 0)
        {
            var message =
                $"Administrator {admin.FullName} extended the deadline for project \"{project.Name}\" from {FormatDate(previousEnd)} to {FormatDate(newEnd)}.\n\nReason:\n{reason}";

            await _notificationService.NotifyUsersAsync(
                engineerUserIds,
                NotificationType.DeadlineExtended,
                "Project deadline extended",
                message,
                "Project",
                projectId);
        }
    }

    public async Task<IReadOnlyList<TaskDeadlineHistoryDto>> GetTaskHistoryAsync(int taskId, int userId, bool isAdmin)
    {
        var task = await _context.Tasks
            .Include(t => t.AssignedEngineer)
            .FirstOrDefaultAsync(t => t.Id == taskId)
            ?? throw new KeyNotFoundException("Task not found.");

        if (!isAdmin)
            await EnsureAssignedEngineerAsync(task, userId);

        return await _context.TaskDeadlineHistories
            .AsNoTracking()
            .Include(h => h.ChangedByUser)
            .Where(h => h.TaskId == taskId)
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new TaskDeadlineHistoryDto
            {
                Id = h.Id,
                TaskId = h.TaskId,
                PreviousStartDate = h.PreviousStartDate,
                NewStartDate = h.NewStartDate,
                PreviousDueDate = h.PreviousDueDate,
                NewDueDate = h.NewDueDate,
                Reason = h.Reason,
                ChangedByUserId = h.ChangedByUserId,
                ChangedByName = h.ChangedByUser.FullName,
                ChangedAt = h.ChangedAt,
                IsAutomatic = h.IsAutomatic
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ProjectDeadlineHistoryDto>> GetProjectHistoryAsync(int projectId, int userId, bool isAdmin)
    {
        _ = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId)
            ?? throw new KeyNotFoundException("Project not found.");

        if (!isAdmin)
            await EnsureProjectAssignedEngineerAsync(projectId, userId);

        return await _context.ProjectDeadlineHistories
            .AsNoTracking()
            .Include(h => h.ChangedByUser)
            .Where(h => h.ProjectId == projectId)
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new ProjectDeadlineHistoryDto
            {
                Id = h.Id,
                ProjectId = h.ProjectId,
                PreviousEndDate = h.PreviousEndDate,
                NewEndDate = h.NewEndDate,
                Reason = h.Reason,
                ChangedByUserId = h.ChangedByUserId,
                ChangedByName = h.ChangedByUser.FullName,
                ChangedAt = h.ChangedAt,
                IsAutomatic = h.IsAutomatic
            })
            .ToListAsync();
    }

    private async Task EnsureAssignedEngineerAsync(TaskItem task, int userId)
    {
        var engineer = await _context.Engineers.FirstOrDefaultAsync(e => e.UserId == userId);
        if (engineer is null || task.AssignedEngineerId != engineer.Id)
            throw new UnauthorizedAccessException("Only the assigned engineer can perform this action.");
    }

    private async Task EnsureProjectAssignedEngineerAsync(int projectId, int userId)
    {
        var engineer = await _context.Engineers.FirstOrDefaultAsync(e => e.UserId == userId);
        if (engineer is null)
            throw new UnauthorizedAccessException("Only assigned engineers can perform this action.");

        var isAssigned = await _context.ProjectAssignments
            .AnyAsync(a => a.ProjectId == projectId && a.EngineerId == engineer.Id);

        if (!isAssigned)
            throw new UnauthorizedAccessException("Only engineers assigned to this project can request an extension.");
    }

    private async Task<User> GetUserAsync(int userId) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Id == userId)
        ?? throw new KeyNotFoundException("User not found.");

    private Task<List<int>> GetAdminUserIdsAsync() =>
        _context.Users
            .Where(u => u.Role == UserRole.Admin && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync();

    private void AddAudit(int userId, string action, string description, string entityType, int entityId)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            Action = action,
            Description = description,
            PerformedByUserId = userId,
            PerformedAt = DateTime.UtcNow,
            EntityType = entityType,
            EntityId = entityId
        });
    }

    private async Task<DeadlineExtensionRequestDto> MapTaskRequestAsync(int id)
    {
        var request = await _context.TaskDeadlineExtensionRequests
            .AsNoTracking()
            .Include(r => r.Task).ThenInclude(t => t.Project)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ReviewedByUser)
            .FirstAsync(r => r.Id == id);

        return MapTaskRequestEntity(request);
    }

    private async Task<DeadlineExtensionRequestDto> MapProjectRequestAsync(int id)
    {
        var request = await _context.ProjectDeadlineExtensionRequests
            .AsNoTracking()
            .Include(r => r.Project)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ReviewedByUser)
            .FirstAsync(r => r.Id == id);

        return MapProjectRequestEntity(request);
    }

    private static DeadlineExtensionRequestDto MapTaskRequestEntity(TaskDeadlineExtensionRequest r) => new()
    {
        Id = r.Id,
        RequestType = "Task",
        TaskId = r.TaskId,
        TaskTitle = r.Task.Title,
        ProjectId = r.Task.ProjectId,
        ProjectName = r.Task.Project.Name,
        RequestedByUserId = r.RequestedByUserId,
        EngineerName = r.RequestedByUser.FullName,
        CurrentDeadline = r.CurrentDueDate,
        RequestedDeadline = r.RequestedDueDate,
        RequestedExtraDays = (r.RequestedDueDate.Date - r.CurrentDueDate.Date).Days,
        Reason = r.Reason,
        Status = r.Status,
        AdminComment = r.AdminComment,
        ReviewedByUserId = r.ReviewedByUserId,
        ReviewedByName = r.ReviewedByUser?.FullName,
        ReviewedAt = r.ReviewedAt,
        CreatedAt = r.CreatedAt
    };

    private static DeadlineExtensionRequestDto MapProjectRequestEntity(ProjectDeadlineExtensionRequest r) => new()
    {
        Id = r.Id,
        RequestType = "Project",
        TaskId = null,
        TaskTitle = null,
        ProjectId = r.ProjectId,
        ProjectName = r.Project.Name,
        RequestedByUserId = r.RequestedByUserId,
        EngineerName = r.RequestedByUser.FullName,
        CurrentDeadline = r.CurrentEndDate,
        RequestedDeadline = r.RequestedEndDate,
        RequestedExtraDays = (r.RequestedEndDate.Date - r.CurrentEndDate.Date).Days,
        Reason = r.Reason,
        Status = r.Status,
        AdminComment = r.AdminComment,
        ReviewedByUserId = r.ReviewedByUserId,
        ReviewedByName = r.ReviewedByUser?.FullName,
        ReviewedAt = r.ReviewedAt,
        CreatedAt = r.CreatedAt
    };

    private static string? NormalizeComment(string? comment) =>
        string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

    private static string FormatDate(DateTime date) => date.ToString("MMM d, yyyy");
}
