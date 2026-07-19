using ConstructionProjectTracker.API.Entities;

namespace ConstructionProjectTracker.API.Interfaces;

/// <summary>
/// Centralized scheduling validation for task dates and dependencies.
/// Controllers and services should call this instead of duplicating rules.
/// </summary>
public interface ITaskSchedulingValidationService
{
    void ValidateTaskDates(
        DateTime projectStart,
        DateTime projectEnd,
        DateTime taskStart,
        DateTime taskDue);

    Task ValidateTaskDatesAgainstProjectAsync(
        int projectId,
        DateTime taskStart,
        DateTime taskDue,
        CancellationToken cancellationToken = default);

    void ValidateDependencyCompatibility(TaskItem dependent, TaskItem prerequisite);

    bool WouldCreateCycle(
        int taskId,
        int dependsOnTaskId,
        IReadOnlyDictionary<int, IReadOnlyList<int>> dependencyMap);

    Task ValidateNewDependencyAsync(
        int taskId,
        int dependsOnTaskId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskItem>> GetValidPrerequisiteCandidatesAsync(
        int taskId,
        CancellationToken cancellationToken = default);
}
