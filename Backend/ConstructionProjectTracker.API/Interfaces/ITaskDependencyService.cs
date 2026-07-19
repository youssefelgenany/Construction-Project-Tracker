using ConstructionProjectTracker.API.DTOs.Tasks;

namespace ConstructionProjectTracker.API.Interfaces;

public interface ITaskDependencyService
{
    Task<IReadOnlyList<TaskDependencyDto>> GetDependenciesAsync(int taskId, int userId, bool isAdmin);

    Task<IReadOnlyList<ValidPrerequisiteTaskDto>> GetValidPrerequisitesAsync(int taskId, int userId, bool isAdmin);

    Task<TaskDependencyDto> AddDependencyAsync(int taskId, CreateTaskDependencyDto dto, int userId, bool isAdmin);

    Task<bool> RemoveDependencyAsync(int taskId, int dependsOnTaskId, int userId, bool isAdmin);
}
