using ConstructionProjectTracker.API.Data;
using ConstructionProjectTracker.API.Entities;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConstructionProjectTracker.API.Services;

public class TaskSchedulingValidationService : ITaskSchedulingValidationService
{
    private readonly ApplicationDbContext _context;

    public TaskSchedulingValidationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public void ValidateTaskDates(
        DateTime projectStart,
        DateTime projectEnd,
        DateTime taskStart,
        DateTime taskDue)
    {
        var pStart = projectStart.Date;
        var pEnd = projectEnd.Date;
        var start = taskStart.Date;
        var due = taskDue.Date;

        if (start > due)
        {
            throw new InvalidOperationException("Task start date must be on or before the due date.");
        }

        if (start < pStart || start > pEnd)
        {
            throw new InvalidOperationException(
                $"Task start date must fall within the project schedule ({pStart:yyyy-MM-dd} to {pEnd:yyyy-MM-dd}).");
        }

        if (due < pStart || due > pEnd)
        {
            throw new InvalidOperationException(
                $"Task due date must fall within the project schedule ({pStart:yyyy-MM-dd} to {pEnd:yyyy-MM-dd}).");
        }
    }

    public async Task ValidateTaskDatesAgainstProjectAsync(
        int projectId,
        DateTime taskStart,
        DateTime taskDue,
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .Where(p => p.Id == projectId)
            .Select(p => new { p.StartDate, p.EndDate })
            .FirstOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            throw new InvalidOperationException("Project does not exist.");
        }

        ValidateTaskDates(project.StartDate, project.EndDate, taskStart, taskDue);
    }

    public void ValidateDependencyCompatibility(TaskItem dependent, TaskItem prerequisite)
    {
        if (dependent.Id == prerequisite.Id)
        {
            throw new InvalidOperationException("A task cannot depend on itself.");
        }

        if (dependent.ProjectId != prerequisite.ProjectId)
        {
            throw new InvalidOperationException("Dependencies must be within the same project.");
        }

        if (prerequisite.StartDate.Date > dependent.StartDate.Date)
        {
            throw new InvalidOperationException(
                "A task cannot depend on a prerequisite that starts after it.");
        }

        if (prerequisite.DueDate.Date > dependent.DueDate.Date)
        {
            throw new InvalidOperationException(
                "A task cannot depend on a prerequisite whose due date is after its own due date.");
        }
    }

    public bool WouldCreateCycle(
        int taskId,
        int dependsOnTaskId,
        IReadOnlyDictionary<int, IReadOnlyList<int>> dependencyMap)
    {
        var visited = new HashSet<int>();
        var stack = new Stack<int>();
        stack.Push(dependsOnTaskId);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current))
            {
                continue;
            }

            if (current == taskId)
            {
                return true;
            }

            if (!dependencyMap.TryGetValue(current, out var prerequisites))
            {
                continue;
            }

            foreach (var prerequisiteId in prerequisites)
            {
                stack.Push(prerequisiteId);
            }
        }

        return false;
    }

    public async Task ValidateNewDependencyAsync(
        int taskId,
        int dependsOnTaskId,
        CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken)
            ?? throw new InvalidOperationException("Task does not exist.");

        var prerequisite = await _context.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == dependsOnTaskId, cancellationToken)
            ?? throw new InvalidOperationException("The prerequisite task does not exist.");

        ValidateDependencyCompatibility(task, prerequisite);

        if (await _context.TaskDependencies.AnyAsync(
                d => d.TaskId == taskId && d.DependsOnTaskId == dependsOnTaskId,
                cancellationToken))
        {
            throw new InvalidOperationException("This dependency already exists.");
        }

        var dependencyMap = await BuildProjectDependencyMapAsync(task.ProjectId, cancellationToken);
        if (WouldCreateCycle(taskId, dependsOnTaskId, dependencyMap))
        {
            throw new InvalidOperationException(
                "Adding this dependency would create a circular dependency chain.");
        }
    }

    public async Task<IReadOnlyList<TaskItem>> GetValidPrerequisiteCandidatesAsync(
        int taskId,
        CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task is null)
        {
            return Array.Empty<TaskItem>();
        }

        var existing = await _context.TaskDependencies
            .AsNoTracking()
            .Where(d => d.TaskId == taskId)
            .Select(d => d.DependsOnTaskId)
            .ToListAsync(cancellationToken);
        var existingSet = existing.ToHashSet();

        var candidates = await _context.Tasks
            .AsNoTracking()
            .Where(t => t.ProjectId == task.ProjectId && t.Id != taskId)
            .Where(t => t.StartDate.Date <= task.StartDate.Date && t.DueDate.Date <= task.DueDate.Date)
            .OrderBy(t => t.StartDate)
            .ThenBy(t => t.Title)
            .ToListAsync(cancellationToken);

        var dependencyMap = await BuildProjectDependencyMapAsync(task.ProjectId, cancellationToken);

        return candidates
            .Where(c => !existingSet.Contains(c.Id))
            .Where(c => !WouldCreateCycle(taskId, c.Id, dependencyMap))
            .ToList();
    }

    private async Task<IReadOnlyDictionary<int, IReadOnlyList<int>>> BuildProjectDependencyMapAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        var rows = await _context.TaskDependencies
            .AsNoTracking()
            .Where(d => d.Task.ProjectId == projectId)
            .Select(d => new { d.TaskId, d.DependsOnTaskId })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.TaskId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<int>)g.Select(x => x.DependsOnTaskId).ToList());
    }
}
