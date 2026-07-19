using ConstructionProjectTracker.API.DTOs.Tasks;

namespace ConstructionProjectTracker.API.Interfaces;

public interface ITaskProgressLogService
{
    Task<IReadOnlyList<TaskProgressLogDto>> GetByTaskIdAsync(int taskId, int userId, bool isAdmin);

    Task<TaskProgressLogDto> CreateAsync(int taskId, CreateTaskProgressLogDto dto, int userId, bool isAdmin);
}
