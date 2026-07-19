using ConstructionProjectTracker.API.DTOs.Common;
using ConstructionProjectTracker.API.DTOs.Engineers;

namespace ConstructionProjectTracker.API.Interfaces;

public interface IEngineerService
{
    Task<PagedResult<EngineerResponseDto>> GetAllAsync(
        string? search,
        string? sortBy,
        bool descending,
        int pageNumber,
        int pageSize);

    Task<EngineerDetailsDto?> GetByIdAsync(int id);

    Task<EngineerResponseDto> CreateAsync(CreateEngineerDto dto);

    Task<EngineerResponseDto?> UpdateAsync(int id, UpdateEngineerDto dto);

    Task<bool> DeleteAsync(int id);

    Task<PagedResult<EngineerWorkloadDto>> GetWorkloadAsync(
        string? search,
        string? workloadLevel,
        int? userId,
        int pageNumber,
        int pageSize);

    Task<PagedResult<EngineerPerformanceDto>> GetPerformanceAsync(
        string? search,
        string? sortBy,
        bool descending,
        int pageNumber,
        int pageSize);

    Task<EngineerPerformanceDetailsDto?> GetPerformanceByIdAsync(int id);
}
