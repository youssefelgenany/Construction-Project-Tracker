using ConstructionProjectTracker.API.Data;
using ConstructionProjectTracker.API.DTOs.Common;
using ConstructionProjectTracker.API.DTOs.Tasks;

namespace ConstructionProjectTracker.API.Interfaces;

public interface ITaskService
{
    Task<PagedResult<TaskResponseDto>> GetAllAsync(
        string? search,
        int? projectId,
        int? engineerId,
        string? priority,
        string? status,
        string? sortBy,
        bool descending,
        int pageNumber,
        int pageSize);

    Task<PagedResult<TaskResponseDto>> GetMyTasksAsync(
        int userId,
        string? search,
        string? priority,
        string? status,
        int pageNumber,
        int pageSize);

    Task<PagedResult<TaskResponseDto>?> GetProjectTasksAsync(
        int projectId,
        int userId,
        bool isAdmin,
        int pageNumber,
        int pageSize);

    Task<TaskDetailsDto?> GetByIdAsync(int id, int userId, bool isAdmin);

    Task<TaskResponseDto> CreateAsync(CreateTaskDto dto);

    Task<TaskResponseDto?> UpdateAsync(int id, UpdateTaskDto dto, int userId, bool isAdmin);

    Task<bool> DeleteAsync(int id);
}
