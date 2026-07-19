using ConstructionProjectTracker.API.Data;
using ConstructionProjectTracker.API.DTOs.Tasks;
using ConstructionProjectTracker.API.Entities;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConstructionProjectTracker.API.Services;

public class TaskDependencyService : ITaskDependencyService
{
    private readonly ApplicationDbContext _context;
    private readonly ITaskScheduleService _scheduleService;
    private readonly ITaskSchedulingValidationService _schedulingValidation;
    private readonly ILogger<TaskDependencyService> _logger;

    public TaskDependencyService(
        ApplicationDbContext context,
        ITaskScheduleService scheduleService,
        ITaskSchedulingValidationService schedulingValidation,
        ILogger<TaskDependencyService> logger)
    {
        _context = context;
        _scheduleService = scheduleService;
        _schedulingValidation = schedulingValidation;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TaskDependencyDto>> GetDependenciesAsync(int taskId, int userId, bool isAdmin)
    {
        var task = await _context.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
        if (task is null)
            return Array.Empty<TaskDependencyDto>();

        if (!await CanAccessTaskAsync(task, userId, isAdmin))
            throw new UnauthorizedAccessException("You do not have permission to view this task's dependencies.");

        return await BuildDependencyDtosAsync(taskId);
    }

    public async Task<IReadOnlyList<ValidPrerequisiteTaskDto>> GetValidPrerequisitesAsync(
        int taskId,
        int userId,
        bool isAdmin)
    {
        var task = await _context.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
        if (task is null)
            return Array.Empty<ValidPrerequisiteTaskDto>();

        if (!await CanAccessTaskAsync(task, userId, isAdmin))
            throw new UnauthorizedAccessException("You do not have permission to view this task's dependencies.");

        var candidates = await _schedulingValidation.GetValidPrerequisiteCandidatesAsync(taskId);
        return candidates
            .Select(t => new ValidPrerequisiteTaskDto
            {
                Id = t.Id,
                Title = t.Title,
                StartDate = t.StartDate,
                DueDate = t.DueDate,
                Status = t.Status
            })
            .ToList();
    }

    public async Task<TaskDependencyDto> AddDependencyAsync(
        int taskId,
        CreateTaskDependencyDto dto,
        int userId,
        bool isAdmin)
    {
        if (!isAdmin)
            throw new UnauthorizedAccessException("Only administrators can manage task dependencies.");

        await _schedulingValidation.ValidateNewDependencyAsync(taskId, dto.DependsOnTaskId);

        var task = await _context.Tasks.FirstAsync(t => t.Id == taskId);

        var dependency = new TaskDependency
        {
            TaskId = taskId,
            DependsOnTaskId = dto.DependsOnTaskId
        };

        _context.TaskDependencies.Add(dependency);
        await _context.SaveChangesAsync();
        await _scheduleService.RefreshProjectBlockingStatesAsync(task.ProjectId);

        _logger.LogInformation(
            "Task dependency added: TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}",
            taskId,
            dto.DependsOnTaskId);

        var items = await BuildDependencyDtosAsync(taskId);
        return items.First(d => d.DependsOnTaskId == dto.DependsOnTaskId);
    }

    public async Task<bool> RemoveDependencyAsync(
        int taskId,
        int dependsOnTaskId,
        int userId,
        bool isAdmin)
    {
        if (!isAdmin)
            throw new UnauthorizedAccessException("Only administrators can manage task dependencies.");

        var dependency = await _context.TaskDependencies
            .Include(d => d.Task)
            .FirstOrDefaultAsync(d => d.TaskId == taskId && d.DependsOnTaskId == dependsOnTaskId);

        if (dependency is null)
            return false;

        var projectId = dependency.Task.ProjectId;
        _context.TaskDependencies.Remove(dependency);
        await _context.SaveChangesAsync();
        await _scheduleService.RefreshProjectBlockingStatesAsync(projectId);

        _logger.LogInformation(
            "Task dependency removed: TaskId={TaskId}, DependsOnTaskId={DependsOnTaskId}",
            taskId,
            dependsOnTaskId);

        return true;
    }

    private async Task<IReadOnlyList<TaskDependencyDto>> BuildDependencyDtosAsync(int taskId)
    {
        return await _context.TaskDependencies
            .AsNoTracking()
            .Where(d => d.TaskId == taskId)
            .Select(d => new TaskDependencyDto
            {
                Id = d.Id,
                TaskId = d.TaskId,
                DependsOnTaskId = d.DependsOnTaskId,
                DependsOnTaskTitle = d.DependsOnTask.Title
            })
            .ToListAsync();
    }

    private async Task<bool> CanAccessTaskAsync(TaskItem task, int userId, bool isAdmin)
    {
        if (isAdmin)
            return true;

        var engineerId = await _context.Engineers
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync();

        return engineerId.HasValue && task.AssignedEngineerId == engineerId.Value;
    }
}
