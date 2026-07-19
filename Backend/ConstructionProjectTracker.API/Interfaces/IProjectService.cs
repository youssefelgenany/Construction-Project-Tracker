using ConstructionProjectTracker.API.DTOs.Common;
using ConstructionProjectTracker.API.DTOs.Projects;

namespace ConstructionProjectTracker.API.Interfaces;

public interface IProjectService
{
    Task<PagedResult<ProjectResponseDto>> GetAllAsync(
        string? search,
        string? sortBy,
        bool descending,
        int pageNumber,
        int pageSize);

    Task<ProjectDetailsDto?> GetByIdAsync(int id);

    Task<ProjectResponseDto> CreateAsync(CreateProjectDto dto);

    Task<ProjectResponseDto?> UpdateAsync(int id, UpdateProjectDto dto);

    Task<bool> DeleteAsync(int id);
}
