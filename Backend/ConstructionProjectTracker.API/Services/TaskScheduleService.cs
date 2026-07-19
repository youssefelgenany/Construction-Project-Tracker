using ConstructionProjectTracker.API.Data;
using ConstructionProjectTracker.API.DTOs.Dashboard;
using ConstructionProjectTracker.API.DTOs.Tasks;
using ConstructionProjectTracker.API.Entities;
using ConstructionProjectTracker.API.Enums;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.EntityFrameworkCore;
using TaskStatus = ConstructionProjectTracker.API.Enums.TaskStatus;

namespace ConstructionProjectTracker.API.Services;

public class TaskScheduleService : ITaskScheduleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TaskScheduleService> _logger;

    public TaskScheduleService(ApplicationDbContext context, ILogger<TaskScheduleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProjectTimelineDto?> GetProjectTimelineAsync(int projectId, int userId, bool isAdmin)
    {
        var project = await _context.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is null)
            return null;

        if (!await CanAccessProjectAsync(projectId, userId, isAdmin))
            return null;

        var tasks = await LoadProjectTasksAsync(projectId, userId, isAdmin);
        var dependencies = await LoadProjectDependenciesAsync(projectId);
        var criticalPathIds = CalculateCriticalPathTaskIds(tasks, dependencies);
        var today = DateTime.UtcNow.Date;

        var timelineTasks = tasks.Select(task =>
        {
            var prerequisiteIds = dependencies
                .Where(d => d.TaskId == task.Id)
                .Select(d => d.DependsOnTaskId)
                .ToList();

            var incompletePrerequisites = tasks
                .Where(t => prerequisiteIds.Contains(t.Id) && t.Status != TaskStatus.Completed)
                .Select(t => new TaskPrerequisiteDto
                {
                    TaskId = t.Id,
                    Title = t.Title,
                    Status = t.Status,
                    IsComplete = false
                })
                .ToList();

            return new TimelineTaskDto
            {
                Id = task.Id,
                Title = task.Title,
                StartDate = task.StartDate,
                DueDate = task.DueDate,
                CompletionPercentage = task.CompletionPercentage,
                Status = task.Status,
                Priority = task.Priority,
                EngineerName = task.AssignedEngineer?.User.FullName,
                IsOverdue = task.Status != TaskStatus.Completed && task.DueDate.Date < today,
                IsCritical = criticalPathIds.Contains(task.Id),
                IsBlocked = task.Status == TaskStatus.Blocked || incompletePrerequisites.Count > 0,
                DependsOnTaskIds = prerequisiteIds,
                IncompletePrerequisites = incompletePrerequisites
            };
        }).ToList();

        return new ProjectTimelineDto
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            ProjectStartDate = project.StartDate,
            ProjectEndDate = project.EndDate,
            Tasks = timelineTasks,
            Dependencies = dependencies
        };
    }

    public async Task<IReadOnlyList<CriticalPathTaskDto>> GetCriticalPathAsync(int projectId, int userId, bool isAdmin)
    {
        var projectExists = await _context.Projects.AsNoTracking().AnyAsync(p => p.Id == projectId);
        if (!projectExists)
            return Array.Empty<CriticalPathTaskDto>();

        if (!await CanAccessProjectAsync(projectId, userId, isAdmin))
            return Array.Empty<CriticalPathTaskDto>();

        var tasks = await LoadProjectTasksAsync(projectId, userId, isAdmin);
        var dependencies = await LoadProjectDependenciesAsync(projectId);
        return BuildCriticalPath(tasks, dependencies);
    }

    public async Task<ScheduleSummaryDto> GetScheduleSummaryAsync(int userId, bool isAdmin)
    {
        var tasksQuery = _context.Tasks.AsNoTracking().AsQueryable();
        var projectsQuery = _context.Projects.AsNoTracking().AsQueryable();

        if (!isAdmin)
        {
            var engineerId = await _context.Engineers
                .AsNoTracking()
                .Where(e => e.UserId == userId)
                .Select(e => (int?)e.Id)
                .FirstOrDefaultAsync();

            if (!engineerId.HasValue)
            {
                return new ScheduleSummaryDto();
            }

            var assignedProjectIds = _context.ProjectAssignments
                .AsNoTracking()
                .Where(pa => pa.EngineerId == engineerId.Value)
                .Select(pa => pa.ProjectId);

            tasksQuery = tasksQuery.Where(t => t.AssignedEngineerId == engineerId.Value);
            projectsQuery = projectsQuery.Where(p => assignedProjectIds.Contains(p.Id));
        }

        var tasks = await tasksQuery.ToListAsync();
        var projects = await projectsQuery.ToListAsync();
        var projectIds = projects.Select(p => p.Id).ToList();

        var allDependencies = await _context.TaskDependencies
            .AsNoTracking()
            .Where(d => projectIds.Contains(d.Task.ProjectId))
            .ToListAsync();

        var criticalTaskIds = new HashSet<int>();
        foreach (var projectId in projectIds)
        {
            var projectTasks = tasks.Where(t => t.ProjectId == projectId).ToList();
            var projectDependencies = allDependencies
                .Where(d => projectTasks.Any(t => t.Id == d.TaskId))
                .Select(d => new TaskDependencyDto
                {
                    Id = d.Id,
                    TaskId = d.TaskId,
                    DependsOnTaskId = d.DependsOnTaskId
                })
                .ToList();

            foreach (var id in CalculateCriticalPathTaskIds(projectTasks, projectDependencies))
            {
                criticalTaskIds.Add(id);
            }
        }

        var today = DateTime.UtcNow.Date;
        var projectsBehindSchedule = projects.Count(project =>
        {
            if (project.Status == ProjectStatus.Completed)
                return false;

            var durationDays = Math.Max(1, (project.EndDate.Date - project.StartDate.Date).Days);
            var elapsedDays = Math.Clamp((today - project.StartDate.Date).Days, 0, durationDays);
            var expectedProgress = elapsedDays / (double)durationDays * 100;
            var daysUntilEnd = (project.EndDate.Date - today).Days;

            return daysUntilEnd <= 14 && project.ProgressPercentage + 15 < expectedProgress;
        });

        return new ScheduleSummaryDto
        {
            BlockedTasksCount = tasks.Count(t => t.Status == TaskStatus.Blocked),
            CriticalTasksCount = tasks.Count(t =>
                criticalTaskIds.Contains(t.Id) && t.Status != TaskStatus.Completed),
            ProjectsBehindScheduleCount = projectsBehindSchedule
        };
    }

    public async Task RefreshProjectBlockingStatesAsync(int projectId)
    {
        var tasks = await _context.Tasks
            .Where(t => t.ProjectId == projectId)
            .ToListAsync();

        if (tasks.Count == 0)
            return;

        var dependencies = await _context.TaskDependencies
            .AsNoTracking()
            .Where(d => d.Task.ProjectId == projectId)
            .ToListAsync();

        var dependencyLookup = dependencies
            .GroupBy(d => d.TaskId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.DependsOnTaskId).ToList());

        var taskLookup = tasks.ToDictionary(t => t.Id);
        var changed = false;

        foreach (var task in tasks)
        {
            if (task.Status is TaskStatus.Completed or TaskStatus.PendingReview)
                continue;

            var prerequisiteIds = dependencyLookup.GetValueOrDefault(task.Id, []);
            var hasDependencies = prerequisiteIds.Count > 0;
            var hasIncompletePrerequisites = prerequisiteIds.Any(id =>
                taskLookup.TryGetValue(id, out var prerequisite) &&
                prerequisite.Status != TaskStatus.Completed);

            if (hasIncompletePrerequisites)
            {
                if (task.Status is TaskStatus.NotStarted or TaskStatus.Ready or TaskStatus.Blocked)
                {
                    if (task.Status != TaskStatus.Blocked)
                    {
                        task.Status = TaskStatus.Blocked;
                        task.UpdatedAt = DateTime.UtcNow;
                        changed = true;
                    }
                }

                continue;
            }

            if (!hasDependencies)
                continue;

            if (task.Status == TaskStatus.Blocked)
            {
                task.Status = TaskStatus.Ready;
                task.UpdatedAt = DateTime.UtcNow;
                changed = true;
            }
            else if (task.Status == TaskStatus.NotStarted)
            {
                task.Status = TaskStatus.Ready;
                task.UpdatedAt = DateTime.UtcNow;
                changed = true;
            }
        }

        if (changed)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Refreshed blocking states for project {ProjectId}", projectId);
        }
    }

    public static bool HasIncompleteDependencies(
        int taskId,
        IReadOnlyDictionary<int, List<int>> dependencyLookup,
        IReadOnlyDictionary<int, TaskItem> taskLookup)
    {
        if (!dependencyLookup.TryGetValue(taskId, out var prerequisiteIds) || prerequisiteIds.Count == 0)
            return false;

        return prerequisiteIds.Any(id =>
            taskLookup.TryGetValue(id, out var prerequisite) &&
            prerequisite.Status != TaskStatus.Completed);
    }

    private async Task<List<TaskItem>> LoadProjectTasksAsync(int projectId, int userId, bool isAdmin)
    {
        var query = _context.Tasks
            .AsNoTracking()
            .Include(t => t.AssignedEngineer!)
                .ThenInclude(e => e.User)
            .Where(t => t.ProjectId == projectId);

        if (!isAdmin)
        {
            var engineerId = await _context.Engineers
                .AsNoTracking()
                .Where(e => e.UserId == userId)
                .Select(e => (int?)e.Id)
                .FirstOrDefaultAsync();

            if (!engineerId.HasValue)
                return [];

            query = query.Where(t => t.AssignedEngineerId == engineerId.Value);
        }

        return await query.OrderBy(t => t.StartDate).ThenBy(t => t.Title).ToListAsync();
    }

    private async Task<List<TaskDependencyDto>> LoadProjectDependenciesAsync(int projectId)
    {
        return await _context.TaskDependencies
            .AsNoTracking()
            .Where(d => d.Task.ProjectId == projectId)
            .Select(d => new TaskDependencyDto
            {
                Id = d.Id,
                TaskId = d.TaskId,
                DependsOnTaskId = d.DependsOnTaskId,
                DependsOnTaskTitle = d.DependsOnTask.Title
            })
            .ToListAsync();
    }

    private async Task<bool> CanAccessProjectAsync(int projectId, int userId, bool isAdmin)
    {
        if (isAdmin)
            return true;

        var engineerId = await _context.Engineers
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync();

        if (!engineerId.HasValue)
            return false;

        return await _context.ProjectAssignments.AnyAsync(pa =>
            pa.ProjectId == projectId && pa.EngineerId == engineerId.Value);
    }

    private static HashSet<int> CalculateCriticalPathTaskIds(
        IReadOnlyList<TaskItem> tasks,
        IReadOnlyList<TaskDependencyDto> dependencies)
    {
        return BuildCriticalPath(tasks, dependencies)
            .Where(t => t.IsCritical)
            .Select(t => t.TaskId)
            .ToHashSet();
    }

    private static List<CriticalPathTaskDto> BuildCriticalPath(
        IReadOnlyList<TaskItem> tasks,
        IReadOnlyList<TaskDependencyDto> dependencies)
    {
        if (tasks.Count == 0)
            return [];

        var durations = tasks.ToDictionary(
            t => t.Id,
            t => Math.Max(1, (t.DueDate.Date - t.StartDate.Date).Days + 1));

        var predecessors = tasks.ToDictionary(t => t.Id, _ => new List<int>());
        var successors = tasks.ToDictionary(t => t.Id, _ => new List<int>());

        foreach (var dependency in dependencies)
        {
            predecessors[dependency.TaskId].Add(dependency.DependsOnTaskId);
            successors[dependency.DependsOnTaskId].Add(dependency.TaskId);
        }

        var earlyStart = tasks.ToDictionary(t => t.Id, _ => 0);
        var earlyFinish = tasks.ToDictionary(t => t.Id, t => durations[t.Id]);

        var indegree = tasks.ToDictionary(
            t => t.Id,
            t => predecessors[t.Id].Count);

        var queue = new Queue<int>(indegree.Where(x => x.Value == 0).Select(x => x.Key));
        var topoOrder = new List<int>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            topoOrder.Add(current);

            foreach (var successorId in successors[current])
            {
                earlyStart[successorId] = Math.Max(
                    earlyStart[successorId],
                    earlyFinish[current]);

                earlyFinish[successorId] = earlyStart[successorId] + durations[successorId];

                indegree[successorId]--;
                if (indegree[successorId] == 0)
                    queue.Enqueue(successorId);
            }
        }

        if (topoOrder.Count != tasks.Count)
        {
            return tasks
                .OrderBy(t => t.StartDate)
                .Select((task, index) => new CriticalPathTaskDto
                {
                    Order = index + 1,
                    TaskId = task.Id,
                    Title = task.Title,
                    StartDate = task.StartDate,
                    DueDate = task.DueDate,
                    DurationDays = durations[task.Id],
                    IsCritical = task.Priority == TaskPriority.Critical,
                    Status = task.Status,
                    CompletionPercentage = task.CompletionPercentage
                })
                .ToList();
        }

        var projectDuration = earlyFinish.Values.DefaultIfEmpty(0).Max();
        var lateFinish = tasks.ToDictionary(t => t.Id, _ => projectDuration);
        var lateStart = tasks.ToDictionary(t => t.Id, t => projectDuration - durations[t.Id]);

        for (var i = topoOrder.Count - 1; i >= 0; i--)
        {
            var current = topoOrder[i];
            if (successors[current].Count == 0)
                continue;

            lateFinish[current] = successors[current].Min(id => lateStart[id]);
            lateStart[current] = lateFinish[current] - durations[current];
        }

        return topoOrder
            .Select((taskId, index) =>
            {
                var task = tasks.First(t => t.Id == taskId);
                var slack = lateStart[taskId] - earlyStart[taskId];

                return new CriticalPathTaskDto
                {
                    Order = index + 1,
                    TaskId = task.Id,
                    Title = task.Title,
                    StartDate = task.StartDate,
                    DueDate = task.DueDate,
                    DurationDays = durations[task.Id],
                    EarlyStartDay = earlyStart[taskId],
                    EarlyFinishDay = earlyFinish[taskId],
                    LateStartDay = lateStart[taskId],
                    LateFinishDay = lateFinish[taskId],
                    SlackDays = slack,
                    IsCritical = slack == 0,
                    Status = task.Status,
                    CompletionPercentage = task.CompletionPercentage
                };
            })
            .ToList();
    }
}
