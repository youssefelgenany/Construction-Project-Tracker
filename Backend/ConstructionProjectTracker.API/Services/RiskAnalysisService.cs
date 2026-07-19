using ConstructionProjectTracker.API.Data;
using ConstructionProjectTracker.API.DTOs.Common;
using ConstructionProjectTracker.API.DTOs.Risks;
using ConstructionProjectTracker.API.DTOs.Tasks;
using ConstructionProjectTracker.API.Entities;
using ConstructionProjectTracker.API.Enums;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConstructionProjectTracker.API.Services;

public class RiskAnalysisService : IRiskAnalysisService
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    private readonly ApplicationDbContext _context;

    public RiskAnalysisService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardRiskSummaryDto> GetDashboardRiskSummaryAsync(int userId, bool isAdmin)
    {
        var taskRisks = await BuildTaskRisksAsync(userId, isAdmin);
        var projectRisks = await BuildProjectRisksAsync(userId, isAdmin, taskRisks);

        return new DashboardRiskSummaryDto
        {
            ProjectsAtRiskCount = projectRisks.Count(p => p.RiskLevel != RiskLevel.None),
            TasksAtRiskCount = taskRisks.Count(t => t.RiskLevel != RiskLevel.None && !t.IsOverdue),
            OverdueTasksCount = taskRisks.Count(t => t.IsOverdue),
            PendingReviewsCount = taskRisks.Count(t => t.Status == ConstructionProjectTracker.API.Enums.TaskStatus.PendingReview),
            Projects = projectRisks
                .Where(p => p.RiskLevel != RiskLevel.None)
                .OrderByDescending(p => p.RiskLevel)
                .ThenByDescending(p => p.OverdueTaskCount)
                .ThenBy(p => p.EndDate)
                .Take(10)
                .ToList()
        };
    }

    public async Task<PagedResult<TaskRiskDto>> GetTaskRisksAsync(
        int userId,
        bool isAdmin,
        string? search,
        int? projectId,
        int? engineerId,
        string? priority,
        string? status,
        string? riskLevel,
        string? sortBy,
        bool descending,
        int pageNumber,
        int pageSize)
    {
        pageNumber = pageNumber < 1 ? DefaultPageNumber : pageNumber;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var items = await BuildTaskRisksAsync(userId, isAdmin, projectId, engineerId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            items = items
                .Where(t =>
                    t.Title.ToLowerInvariant().Contains(term) ||
                    t.Description.ToLowerInvariant().Contains(term) ||
                    t.ProjectName.ToLowerInvariant().Contains(term) ||
                    (t.EngineerName?.ToLowerInvariant().Contains(term) ?? false))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(priority) &&
            Enum.TryParse<TaskPriority>(priority, true, out var priorityFilter))
        {
            items = items.Where(t => t.Priority == priorityFilter).ToList();
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<ConstructionProjectTracker.API.Enums.TaskStatus>(status, true, out var statusFilter))
        {
            items = items.Where(t => t.Status == statusFilter).ToList();
        }

        if (!string.IsNullOrWhiteSpace(riskLevel) &&
            Enum.TryParse<RiskLevel>(riskLevel, true, out var riskFilter))
        {
            items = items.Where(t => t.RiskLevel == riskFilter).ToList();
        }

        items = ApplyTaskSorting(items, sortBy, descending);

        var totalCount = items.Count;
        var pagedItems = items
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<TaskRiskDto>
        {
            Items = pagedItems,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<PagedResult<ProjectRiskDto>> GetProjectRisksAsync(
        int userId,
        bool isAdmin,
        string? search,
        string? riskLevel,
        string? sortBy,
        bool descending,
        int pageNumber,
        int pageSize)
    {
        pageNumber = pageNumber < 1 ? DefaultPageNumber : pageNumber;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var taskRisks = await BuildTaskRisksAsync(userId, isAdmin);
        var items = await BuildProjectRisksAsync(userId, isAdmin, taskRisks);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            items = items
                .Where(p =>
                    p.Name.ToLowerInvariant().Contains(term) ||
                    p.Description.ToLowerInvariant().Contains(term) ||
                    p.Reason.ToLowerInvariant().Contains(term))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(riskLevel) &&
            Enum.TryParse<RiskLevel>(riskLevel, true, out var riskFilter))
        {
            items = items.Where(p => p.RiskLevel == riskFilter).ToList();
        }

        items = ApplyProjectSorting(items, sortBy, descending);

        var totalCount = items.Count;
        var pagedItems = items
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<ProjectRiskDto>
        {
            Items = pagedItems,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private async Task<List<TaskRiskDto>> BuildTaskRisksAsync(
        int userId,
        bool isAdmin,
        int? projectId = null,
        int? engineerId = null)
    {
        var query = _context.Tasks
            .AsNoTracking()
            .Include(t => t.Project)
            .Include(t => t.AssignedEngineer!)
                .ThenInclude(e => e.User)
            .Include(t => t.ProgressLogs)
            .Include(t => t.CompletionReport!)
                .ThenInclude(r => r.UploadedByUser)
            .AsQueryable();

        if (projectId.HasValue)
        {
            query = query.Where(t => t.ProjectId == projectId.Value);
        }

        if (isAdmin)
        {
            if (engineerId.HasValue)
            {
                query = query.Where(t => t.AssignedEngineerId == engineerId.Value);
            }
        }
        else
        {
            var currentEngineerId = await _context.Engineers
                .AsNoTracking()
                .Where(e => e.UserId == userId)
                .Select(e => (int?)e.Id)
                .FirstOrDefaultAsync();

            if (!currentEngineerId.HasValue)
            {
                return [];
            }

            query = query.Where(t => t.AssignedEngineerId == currentEngineerId.Value);
        }

        var tasks = await query.ToListAsync();
        return tasks
            .Select(EvaluateTaskRisk)
            .ToList();
    }

    private async Task<List<ProjectRiskDto>> BuildProjectRisksAsync(
        int userId,
        bool isAdmin,
        IReadOnlyCollection<TaskRiskDto>? precomputedTaskRisks = null)
    {
        var query = _context.Projects
            .AsNoTracking()
            .Include(p => p.ProjectAssignments)
                .ThenInclude(pa => pa.Engineer)
            .Include(p => p.Tasks)
            .AsQueryable();

        if (!isAdmin)
        {
            var currentEngineerId = await _context.Engineers
                .AsNoTracking()
                .Where(e => e.UserId == userId)
                .Select(e => (int?)e.Id)
                .FirstOrDefaultAsync();

            if (!currentEngineerId.HasValue)
            {
                return [];
            }

            query = query.Where(p => p.ProjectAssignments.Any(pa => pa.EngineerId == currentEngineerId.Value));
        }

        var projects = await query.ToListAsync();
        var taskRiskLookup = (precomputedTaskRisks ?? await BuildTaskRisksAsync(userId, isAdmin))
            .GroupBy(t => t.ProjectId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return projects
            .Select(project =>
            {
                var projectTaskRisks = taskRiskLookup.GetValueOrDefault(project.Id, []);
                return EvaluateProjectRisk(project, projectTaskRisks);
            })
            .ToList();
    }

    private static TaskRiskDto EvaluateTaskRisk(TaskItem task)
    {
        var today = DateTime.UtcNow.Date;
        var dueDate = task.DueDate.Date;
        var startDate = task.StartDate.Date;
        var orderedLogs = task.ProgressLogs.OrderByDescending(log => log.CreatedAt).ToList();
        var lastLog = orderedLogs.FirstOrDefault();
        var lastMeaningfulUpdate = orderedLogs.FirstOrDefault(log => log.NewProgress != log.PreviousProgress);

        var dto = new TaskRiskDto
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            ProjectName = task.Project.Name,
            AssignedEngineerId = task.AssignedEngineerId,
            EngineerName = task.AssignedEngineer?.User.FullName,
            Title = task.Title,
            Description = task.Description,
            Priority = task.Priority,
            CompletionPercentage = task.CompletionPercentage,
            Status = task.Status,
            StartDate = task.StartDate,
            DueDate = task.DueDate,
            CompletionReport = task.CompletionReport is null ? null : new TaskCompletionReportDto
            {
                Id = task.CompletionReport.Id,
                TaskId = task.CompletionReport.TaskId,
                OriginalFileName = task.CompletionReport.OriginalFileName,
                Extension = task.CompletionReport.Extension,
                ContentType = task.CompletionReport.ContentType,
                FileSize = task.CompletionReport.FileSize,
                UploadedAt = task.CompletionReport.UploadedAt,
                UploadedBy = task.CompletionReport.UploadedByUser.FullName,
                RejectionComment = task.CompletionReport.RejectionComment
            },
            RiskLevel = RiskLevel.None,
            Reason = "On schedule.",
            SuggestedAction = "Continue monitoring progress.",
            IsOverdue = false,
            DaysOverdue = 0
        };

        if (task.Status != ConstructionProjectTracker.API.Enums.TaskStatus.Completed && dueDate < today)
        {
            dto.IsOverdue = true;
            dto.DaysOverdue = (today - dueDate).Days;
            dto.RiskLevel = task.Priority == TaskPriority.Critical ? RiskLevel.Critical : RiskLevel.High;
            dto.Reason = $"Overdue by {dto.DaysOverdue} day{Pluralize(dto.DaysOverdue)}.";
            dto.SuggestedAction = "Escalate immediately and recover the schedule.";
            return dto;
        }

        if (task.Status == ConstructionProjectTracker.API.Enums.TaskStatus.PendingReview &&
            task.CompletionReport is not null &&
            (today - task.CompletionReport.UploadedAt.Date).Days > 3)
        {
            var pendingDays = (today - task.CompletionReport.UploadedAt.Date).Days;
            dto.RiskLevel = RiskLevel.High;
            dto.Reason = $"Completion report has been pending review for {pendingDays} day{Pluralize(pendingDays)}.";
            dto.SuggestedAction = "Review the completion report to unblock task closure.";
            return dto;
        }

        var totalDurationDays = Math.Max(1, (dueDate - startDate).Days);
        var elapsedDays = Math.Clamp((today - startDate).Days, 0, totalDurationDays);
        var elapsedRatio = elapsedDays / (double)totalDurationDays;
        var expectedProgress = Math.Round(elapsedRatio * 100, 0);
        var progressGap = expectedProgress - task.CompletionPercentage;
        var daysUntilDue = (dueDate - today).Days;

        if (elapsedRatio >= 0.75 && progressGap >= 20)
        {
            var level = progressGap >= 35 || daysUntilDue <= 3 ? RiskLevel.High : RiskLevel.Medium;
            dto.RiskLevel = level;
            dto.Reason =
                $"Time elapsed is {Math.Round(elapsedRatio * 100)}% but progress is only {task.CompletionPercentage}%.";
            dto.SuggestedAction = "Re-plan the task and add immediate progress updates.";
            return dto;
        }

        var lastActivityDate = lastLog?.CreatedAt.Date ?? task.UpdatedAt.Date;
        var idleDays = (today - lastActivityDate).Days;
        if (task.Status != ConstructionProjectTracker.API.Enums.TaskStatus.Completed && idleDays > 7)
        {
            dto.RiskLevel = daysUntilDue <= 7 ? RiskLevel.High : RiskLevel.Medium;
            dto.Reason = $"No progress updates have been posted for {idleDays} days.";
            dto.SuggestedAction = "Request a status update and confirm blockers.";
            return dto;
        }

        if (task.CompletionPercentage > 0 &&
            task.CompletionPercentage < 100 &&
            lastMeaningfulUpdate is not null &&
            (today - lastMeaningfulUpdate.CreatedAt.Date).Days > 7)
        {
            var stuckDays = (today - lastMeaningfulUpdate.CreatedAt.Date).Days;
            dto.RiskLevel = daysUntilDue <= 7 ? RiskLevel.High : RiskLevel.Medium;
            dto.Reason = $"Progress has been stuck at {task.CompletionPercentage}% for {stuckDays} days.";
            dto.SuggestedAction = "Review blockers and redistribute work if needed.";
            return dto;
        }

        if (daysUntilDue <= 3 && task.Status != ConstructionProjectTracker.API.Enums.TaskStatus.Completed && task.CompletionPercentage < 60)
        {
            dto.RiskLevel = RiskLevel.Low;
            dto.Reason = $"Due in {Math.Max(daysUntilDue, 0)} day{Pluralize(Math.Max(daysUntilDue, 0))} with only {task.CompletionPercentage}% progress.";
            dto.SuggestedAction = "Increase follow-up frequency until the due date.";
        }

        return dto;
    }

    private static ProjectRiskDto EvaluateProjectRisk(Project project, IReadOnlyCollection<TaskRiskDto> taskRisks)
    {
        var today = DateTime.UtcNow.Date;
        var activeTasks = taskRisks
            .Where(t => t.Status != ConstructionProjectTracker.API.Enums.TaskStatus.Completed)
            .ToList();
        var atRiskActiveTasks = activeTasks.Where(t => t.RiskLevel >= RiskLevel.Medium).ToList();
        var overdueTasks = taskRisks.Where(t => t.IsOverdue).ToList();
        var hasCriticalOverdueTask = overdueTasks.Any(t => t.Priority == TaskPriority.Critical);

        var dto = new ProjectRiskDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Budget = project.Budget,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Status = project.Status,
            ProgressPercentage = project.ProgressPercentage,
            RiskLevel = RiskLevel.None,
            Reason = "Healthy schedule.",
            SuggestedAction = "Continue regular project monitoring.",
            ActiveTaskCount = activeTasks.Count(),
            AtRiskTaskCount = atRiskActiveTasks.Count(),
            OverdueTaskCount = overdueTasks.Count(),
            HasCriticalOverdueTask = hasCriticalOverdueTask
        };

        if (taskRisks.Count == 0)
        {
            dto.RiskLevel = RiskLevel.None;
            dto.Reason = "This project does not yet contain any scheduled tasks.";
            dto.SuggestedAction = "Add and schedule project tasks to begin delivery tracking.";
            return dto;
        }

        if (hasCriticalOverdueTask)
        {
            dto.RiskLevel = RiskLevel.Critical;
            dto.Reason = "A critical task is overdue.";
            dto.SuggestedAction = "Escalate immediately and assign recovery actions.";
            return dto;
        }

        if (activeTasks.Count() > 0)
        {
            var atRiskRatio = atRiskActiveTasks.Count() / (double)activeTasks.Count();
            if (atRiskRatio > 0.25)
            {
                dto.RiskLevel = atRiskRatio >= 0.5 ? RiskLevel.High : RiskLevel.Medium;
                dto.Reason = $"{atRiskActiveTasks.Count()} of {activeTasks.Count()} active tasks are at risk.";
                dto.SuggestedAction = "Review task dependencies and recovery plans with the team.";
                return dto;
            }
        }

        var projectDurationDays = Math.Max(1, (project.EndDate.Date - project.StartDate.Date).Days);
        var elapsedProjectDays = Math.Clamp((today - project.StartDate.Date).Days, 0, projectDurationDays);
        var projectElapsedRatio = elapsedProjectDays / (double)projectDurationDays;
        var expectedProjectProgress = Math.Round(projectElapsedRatio * 100, 0);
        var daysUntilEnd = (project.EndDate.Date - today).Days;

        if (daysUntilEnd <= 14 && project.ProgressPercentage + 15 < expectedProjectProgress)
        {
            dto.RiskLevel = daysUntilEnd <= 7 ? RiskLevel.High : RiskLevel.Medium;
            dto.Reason =
                $"Project is {project.ProgressPercentage}% complete with {Math.Max(daysUntilEnd, 0)} day{Pluralize(Math.Max(daysUntilEnd, 0))} remaining.";
            dto.SuggestedAction = "Rebaseline the schedule and prioritize delayed deliverables.";
            return dto;
        }

        if (overdueTasks.Count > 0 || taskRisks.Any(t => t.RiskLevel == RiskLevel.Low))
        {
            dto.RiskLevel = RiskLevel.Low;
            dto.Reason = overdueTasks.Count > 0
                ? $"{overdueTasks.Count} task{Pluralize(overdueTasks.Count)} need immediate follow-up."
                : "Early warning signs detected in task execution.";
            dto.SuggestedAction = "Monitor closely and verify upcoming deadlines.";
        }

        return dto;
    }

    private static List<TaskRiskDto> ApplyTaskSorting(IEnumerable<TaskRiskDto> items, string? sortBy, bool descending)
    {
        var ordered = sortBy?.ToLowerInvariant() switch
        {
            "risk" or "highestrisk" => descending
                ? items.OrderBy(t => t.RiskLevel).ThenBy(t => t.DueDate)
                : items.OrderByDescending(t => t.RiskLevel).ThenByDescending(t => t.DaysOverdue).ThenBy(t => t.DueDate),
            "overdue" or "mostoverdue" => descending
                ? items.OrderBy(t => t.DaysOverdue).ThenBy(t => t.DueDate)
                : items.OrderByDescending(t => t.DaysOverdue).ThenByDescending(t => t.RiskLevel).ThenBy(t => t.DueDate),
            "deadline" or "closestdeadline" => descending
                ? items.OrderByDescending(t => t.DueDate).ThenByDescending(t => t.RiskLevel)
                : items.OrderBy(t => t.DueDate).ThenByDescending(t => t.RiskLevel),
            "startdate" => descending
                ? items.OrderByDescending(t => t.StartDate).ThenBy(t => t.Title)
                : items.OrderBy(t => t.StartDate).ThenBy(t => t.Title),
            "duedate" => descending
                ? items.OrderByDescending(t => t.DueDate).ThenBy(t => t.Title)
                : items.OrderBy(t => t.DueDate).ThenBy(t => t.Title),
            _ => descending
                ? items.OrderByDescending(t => t.Title)
                : items.OrderBy(t => t.Title)
        };

        return ordered.ToList();
    }

    private static List<ProjectRiskDto> ApplyProjectSorting(IEnumerable<ProjectRiskDto> items, string? sortBy, bool descending)
    {
        var ordered = sortBy?.ToLowerInvariant() switch
        {
            "risk" or "highestrisk" => descending
                ? items.OrderBy(p => p.RiskLevel).ThenBy(p => p.EndDate)
                : items.OrderByDescending(p => p.RiskLevel).ThenByDescending(p => p.OverdueTaskCount).ThenBy(p => p.EndDate),
            "overdue" or "mostoverdue" => descending
                ? items.OrderBy(p => p.OverdueTaskCount).ThenBy(p => p.EndDate)
                : items.OrderByDescending(p => p.OverdueTaskCount).ThenByDescending(p => p.RiskLevel).ThenBy(p => p.EndDate),
            "deadline" or "closestdeadline" => descending
                ? items.OrderByDescending(p => p.EndDate).ThenByDescending(p => p.RiskLevel)
                : items.OrderBy(p => p.EndDate).ThenByDescending(p => p.RiskLevel),
            _ => descending
                ? items.OrderByDescending(p => p.Name)
                : items.OrderBy(p => p.Name)
        };

        return ordered.ToList();
    }

    private static string Pluralize(int value) => value == 1 ? string.Empty : "s";
}
