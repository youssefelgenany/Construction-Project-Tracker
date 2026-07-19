using ConstructionProjectTracker.API.Data;
using ConstructionProjectTracker.API.DTOs.DeadlineExtensions;
using ConstructionProjectTracker.API.Entities;
using ConstructionProjectTracker.API.Enums;
using ConstructionProjectTracker.API.Helpers;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConstructionProjectTracker.API.Services;

public class TaskDeadlineCascadeService : ITaskDeadlineCascadeService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public TaskDeadlineCascadeService(
        ApplicationDbContext context,
        INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<ScheduleImpactAnalysisDto> AnalyzeAsync(int taskId, AnalyzeTaskDeadlineExtensionDto dto)
    {
        var (graph, source, project) = await LoadGraphAsync(taskId);
        ValidateExtensionInput(source, dto.NewDueDate, dto.Reason);

        var plan = BuildCascadePlan(graph, source.Id, dto.NewDueDate.Date, project.EndDate.Date);

        return ToAnalysisDto(source, project, dto.NewDueDate.Date, dto.Reason.Trim(), plan);
    }

    public async Task<ApplyTaskDeadlineExtensionResultDto> ApplyAsync(
        int taskId,
        ApplyTaskDeadlineExtensionDto dto,
        int adminUserId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var staged = await StageApplyAsync(taskId, dto, adminUserId);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            await NotifyStagedApplyAsync(staged);
            return staged.Result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<StagedTaskDeadlineExtensionDto> StageApplyAsync(
        int taskId,
        ApplyTaskDeadlineExtensionDto dto,
        int adminUserId)
    {
        var (graph, source, projectSnapshot) = await LoadGraphAsync(taskId);
        ValidateExtensionInput(source, dto.NewDueDate, dto.Reason);

        var newDue = dto.NewDueDate.Date;
        var reason = dto.Reason.Trim();
        var plan = BuildCascadePlan(graph, source.Id, newDue, projectSnapshot.EndDate.Date);

        if (plan.RequiresProjectExtension && !dto.ConfirmProjectExtension)
        {
            throw new InvalidOperationException(
                "This extension also requires extending the project deadline. Confirm the project extension to continue.");
        }

        var admin = await _context.Users.FirstAsync(u => u.Id == adminUserId);
        var now = DateTime.UtcNow;
        var previousSourceDue = source.DueDate;

        // Apply source task due date on a tracked entity
        var sourceEntity = await _context.Tasks.FirstAsync(t => t.Id == source.Id);
        sourceEntity.DueDate = newDue;
        sourceEntity.UpdatedAt = now;

        _context.TaskDeadlineHistories.Add(new TaskDeadlineHistory
        {
            TaskId = source.Id,
            PreviousDueDate = previousSourceDue,
            NewDueDate = newDue,
            Reason = reason,
            ChangedByUserId = adminUserId,
            ChangedAt = now,
            IsAutomatic = false
        });

        AddAudit(
            adminUserId,
            "TaskDeadlineExtended",
            $"Administrator {admin.FullName} extended the deadline for task \"{source.Title}\" from {FormatDate(previousSourceDue)} to {FormatDate(newDue)}.",
            "Task",
            source.Id);

        // Apply dependent shifts on tracked entities
        foreach (var shift in plan.AffectedTasks)
        {
            var taskEntity = await _context.Tasks.FirstAsync(t => t.Id == shift.TaskId);
            taskEntity.StartDate = shift.NewStart;
            taskEntity.DueDate = shift.NewDue;
            taskEntity.UpdatedAt = now;

            _context.TaskDeadlineHistories.Add(new TaskDeadlineHistory
            {
                TaskId = shift.TaskId,
                PreviousStartDate = shift.CurrentStart,
                NewStartDate = shift.NewStart,
                PreviousDueDate = shift.CurrentDue,
                NewDueDate = shift.NewDue,
                Reason =
                    $"Automatically rescheduled because \"{source.Title}\" was extended. Original reason: {reason}",
                ChangedByUserId = adminUserId,
                ChangedAt = now,
                IsAutomatic = true
            });

            AddAudit(
                adminUserId,
                "TaskScheduleShifted",
                $"Administrator {admin.FullName} automatically shifted task \"{shift.Title}\" from {FormatDate(shift.CurrentStart)}–{FormatDate(shift.CurrentDue)} to {FormatDate(shift.NewStart)}–{FormatDate(shift.NewDue)} due to dependency on \"{source.Title}\".",
                "Task",
                shift.TaskId);
        }

        var projectExtended = false;
        DateTime? newProjectEnd = null;
        if (plan.RequiresProjectExtension)
        {
            // CRITICAL: Load the tracked Project entity — LoadGraphAsync uses AsNoTracking.
            var projectEntity = await _context.Projects.FirstAsync(p => p.Id == projectSnapshot.Id);
            var previousEnd = projectEntity.EndDate;
            projectEntity.EndDate = plan.NewRequiredProjectEnd;
            newProjectEnd = plan.NewRequiredProjectEnd;
            projectExtended = true;

            _context.ProjectDeadlineHistories.Add(new ProjectDeadlineHistory
            {
                ProjectId = projectEntity.Id,
                PreviousEndDate = previousEnd,
                NewEndDate = plan.NewRequiredProjectEnd,
                Reason =
                    $"Automatically extended because task \"{source.Title}\" and dependent tasks required a later project end. Original reason: {reason}",
                ChangedByUserId = adminUserId,
                ChangedAt = now,
                IsAutomatic = true
            });

            AddAudit(
                adminUserId,
                "ProjectDeadlineExtended",
                $"Administrator {admin.FullName} automatically extended project \"{projectEntity.Name}\" from {FormatDate(previousEnd)} to {FormatDate(plan.NewRequiredProjectEnd)} to accommodate schedule changes from \"{source.Title}\".",
                "Project",
                projectEntity.Id);
        }

        return new StagedTaskDeadlineExtensionDto
        {
            Result = new ApplyTaskDeadlineExtensionResultDto
            {
                Message = plan.AffectedTasks.Count == 0
                    ? "Task deadline extended successfully."
                    : $"Task deadline extended and {plan.AffectedTasks.Count} dependent task(s) rescheduled.",
                ShiftedTaskCount = plan.AffectedTasks.Count,
                ProjectExtended = projectExtended,
                NewProjectEndDate = newProjectEnd
            },
            AdminUserId = adminUserId,
            SourceTaskId = source.Id,
            SourceTaskTitle = source.Title,
            SourceEngineerUserId = source.AssignedEngineerUserId,
            PreviousSourceDue = previousSourceDue,
            NewSourceDue = newDue,
            Reason = reason,
            ProjectId = projectSnapshot.Id,
            ProjectName = projectSnapshot.Name,
            ProjectExtended = projectExtended,
            NewProjectEndDate = newProjectEnd,
            AffectedTasks = plan.AffectedTasks
        };
    }

    public async Task NotifyStagedApplyAsync(StagedTaskDeadlineExtensionDto staged)
    {
        var admin = await _context.Users.FirstAsync(u => u.Id == staged.AdminUserId);

        if (staged.SourceEngineerUserId is int sourceEngineerId)
        {
            await _notificationService.NotifyUsersAsync(
                [sourceEngineerId],
                NotificationType.DeadlineExtended,
                "Task deadline extended",
                $"Administrator {admin.FullName} extended the deadline for task \"{staged.SourceTaskTitle}\" from {FormatDate(staged.PreviousSourceDue)} to {FormatDate(staged.NewSourceDue)}.\n\nReason:\n{staged.Reason}",
                "Task",
                staged.SourceTaskId);
        }

        foreach (var shift in staged.AffectedTasks)
        {
            if (shift.AssignedEngineerUserId is not int engineerUserId)
                continue;

            var message =
                $"Administrator {admin.FullName} extended the deadline of task \"{staged.SourceTaskTitle}\".\n\n" +
                $"Because your task depends on it, your task \"{shift.Title}\" has automatically been moved.\n\n" +
                $"New schedule\nStart\n{FormatDate(shift.NewStart)}\n\nDue\n{FormatDate(shift.NewDue)}";

            await _notificationService.NotifyUsersAsync(
                [engineerUserId],
                NotificationType.DeadlineExtended,
                "Task schedule updated",
                message,
                "Task",
                shift.TaskId);
        }

        if (!staged.ProjectExtended || staged.NewProjectEndDate is null)
            return;

        var engineerIds = await _context.ProjectAssignments
            .Where(a => a.ProjectId == staged.ProjectId)
            .Select(a => a.Engineer.UserId)
            .Distinct()
            .ToListAsync();

        if (engineerIds.Count == 0)
            return;

        await _notificationService.NotifyUsersAsync(
            engineerIds,
            NotificationType.DeadlineExtended,
            "Project deadline extended",
            $"Administrator {admin.FullName} extended the deadline for project \"{staged.ProjectName}\" to {FormatDate(staged.NewProjectEndDate.Value)} to accommodate schedule changes from \"{staged.SourceTaskTitle}\".",
            "Project",
            staged.ProjectId);
    }

    private async Task<(
        Dictionary<int, CascadeTaskNode> Graph,
        CascadeTaskNode Source,
        Project Project)> LoadGraphAsync(int taskId)
    {
        var sourceTask = await _context.Tasks
            .AsNoTracking()
            .Include(t => t.Project)
            .Include(t => t.AssignedEngineer)!.ThenInclude(e => e!.User)
            .FirstOrDefaultAsync(t => t.Id == taskId)
            ?? throw new KeyNotFoundException("Task not found.");

        var projectTasks = await _context.Tasks
            .AsNoTracking()
            .Include(t => t.AssignedEngineer)!.ThenInclude(e => e!.User)
            .Where(t => t.ProjectId == sourceTask.ProjectId)
            .ToListAsync();

        var taskIds = projectTasks.Select(t => t.Id).ToList();

        var dependencies = await _context.TaskDependencies
            .AsNoTracking()
            .Where(d => taskIds.Contains(d.TaskId) || taskIds.Contains(d.DependsOnTaskId))
            .ToListAsync();

        var successors = taskIds.ToDictionary(id => id, _ => new List<int>());
        foreach (var edge in dependencies)
        {
            if (!successors.ContainsKey(edge.DependsOnTaskId))
                successors[edge.DependsOnTaskId] = [];
            successors[edge.DependsOnTaskId].Add(edge.TaskId);
        }

        var graph = projectTasks.ToDictionary(
            t => t.Id,
            t => new CascadeTaskNode
            {
                Id = t.Id,
                Title = t.Title,
                StartDate = t.StartDate.Date,
                DueDate = t.DueDate.Date,
                AssignedEngineerUserId = t.AssignedEngineer?.UserId,
                EngineerName = t.AssignedEngineer?.User?.FullName,
                SuccessorIds = successors.GetValueOrDefault(t.Id) ?? []
            });

        return (graph, graph[sourceTask.Id], sourceTask.Project);
    }

    private static void ValidateExtensionInput(CascadeTaskNode source, DateTime newDueDate, string reason)
    {
        if (newDueDate.Date <= source.DueDate.Date)
            throw new InvalidOperationException("New due date must be after the current due date.");

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10)
            throw new InvalidOperationException("Reason for extension is required.");
    }

    private static CascadePlan BuildCascadePlan(
        Dictionary<int, CascadeTaskNode> graph,
        int sourceTaskId,
        DateTime proposedSourceDue,
        DateTime currentProjectEnd)
    {
        // STEP 1 — Simulate extending the selected task in memory (no DB writes).
        var starts = graph.ToDictionary(kv => kv.Key, kv => kv.Value.StartDate);
        var simulatedDues = graph.ToDictionary(kv => kv.Key, kv => kv.Value.DueDate);
        simulatedDues[sourceTaskId] = proposedSourceDue.Date;

        // STEP 2 — Recursively resolve every successor dependency; shift only when required.
        var shifts = new Dictionary<int, ScheduleImpactTaskDto>();
        var queue = new Queue<int>();
        queue.Enqueue(sourceTaskId);

        while (queue.Count > 0)
        {
            var parentId = queue.Dequeue();
            var parentDue = simulatedDues[parentId];
            var requiredStart = WorkingDaysCalculator.AddWorkingDays(parentDue, 1);

            foreach (var childId in graph[parentId].SuccessorIds)
            {
                if (!graph.ContainsKey(childId))
                    continue;

                var childStart = starts[childId];
                if (childStart >= requiredStart)
                    continue;

                var shiftDays = WorkingDaysCalculator.GetWorkingDaysShift(childStart, requiredStart);
                if (shiftDays <= 0)
                    continue;

                var node = graph[childId];
                var originalStart = node.StartDate;
                var originalDue = node.DueDate;
                if (shifts.TryGetValue(childId, out var existingShift))
                {
                    originalStart = existingShift.CurrentStart;
                    originalDue = existingShift.CurrentDue;
                }

                var newStart = WorkingDaysCalculator.AddWorkingDays(starts[childId], shiftDays);
                var newDue = WorkingDaysCalculator.AddWorkingDays(simulatedDues[childId], shiftDays);

                starts[childId] = newStart;
                simulatedDues[childId] = newDue;

                shifts[childId] = new ScheduleImpactTaskDto
                {
                    TaskId = childId,
                    Title = node.Title,
                    CurrentStart = originalStart,
                    CurrentDue = originalDue,
                    NewStart = newStart,
                    NewDue = newDue,
                    DaysShifted = WorkingDaysCalculator.GetWorkingDaysShift(originalStart, newStart),
                    AssignedEngineerUserId = node.AssignedEngineerUserId,
                    EngineerName = node.EngineerName
                };

                queue.Enqueue(childId);
            }
        }

        // STEP 3 — Project impact from the final simulated schedule only.
        // LatestTaskDueDate = MAX(due date of every project task after simulation).
        // Never derive project extension from the edited task alone.
        var latestTaskDueDate = simulatedDues.Count == 0
            ? proposedSourceDue.Date
            : simulatedDues.Values.Max();

        var requiresProjectExtension = latestTaskDueDate > currentProjectEnd.Date;
        var affected = shifts.Values
            .OrderBy(t => t.NewStart)
            .ThenBy(t => t.Title)
            .ToList();

        return new CascadePlan
        {
            AffectedTasks = affected,
            LatestTaskDueDate = latestTaskDueDate,
            RequiresProjectExtension = requiresProjectExtension,
            // Only meaningful when RequiresProjectExtension is true.
            NewRequiredProjectEnd = requiresProjectExtension ? latestTaskDueDate : currentProjectEnd.Date,
            TotalShiftWorkingDays = affected.Sum(t => t.DaysShifted)
        };
    }

    private static ScheduleImpactAnalysisDto ToAnalysisDto(
        CascadeTaskNode source,
        Project project,
        DateTime proposedDue,
        string reason,
        CascadePlan plan)
    {
        var hasDependencyConflicts = plan.AffectedTasks.Count > 0;
        var hasScheduleImpact = hasDependencyConflicts || plan.RequiresProjectExtension;

        return new ScheduleImpactAnalysisDto
        {
            SourceTaskId = source.Id,
            SourceTaskTitle = source.Title,
            CurrentDueDate = source.DueDate,
            ProposedDueDate = proposedDue,
            Reason = reason,
            HasConflicts = hasScheduleImpact,
            AffectedTasks = plan.AffectedTasks,
            AffectedTaskCount = plan.AffectedTasks.Count,
            TotalShiftWorkingDays = plan.TotalShiftWorkingDays,
            LatestTaskDueDate = plan.LatestTaskDueDate,
            CurrentProjectEnd = project.EndDate.Date,
            NewRequiredProjectEnd = plan.NewRequiredProjectEnd,
            RequiresProjectExtension = plan.RequiresProjectExtension,
            ProjectId = project.Id,
            ProjectName = project.Name
        };
    }

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

    private static string FormatDate(DateTime date) => date.ToString("MMM d, yyyy");

    private sealed class CascadeTaskNode
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public DateTime StartDate { get; init; }
        public DateTime DueDate { get; init; }
        public int? AssignedEngineerUserId { get; init; }
        public string? EngineerName { get; init; }
        public List<int> SuccessorIds { get; init; } = [];
    }

    private sealed class CascadePlan
    {
        public List<ScheduleImpactTaskDto> AffectedTasks { get; init; } = [];
        public DateTime LatestTaskDueDate { get; init; }
        public bool RequiresProjectExtension { get; init; }
        public DateTime NewRequiredProjectEnd { get; init; }
        public int TotalShiftWorkingDays { get; init; }
    }
}
