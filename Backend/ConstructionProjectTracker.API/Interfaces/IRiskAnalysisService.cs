using ConstructionProjectTracker.API.DTOs.Common;
using ConstructionProjectTracker.API.DTOs.Risks;

namespace ConstructionProjectTracker.API.Interfaces;

public interface IRiskAnalysisService
{
    Task<DashboardRiskSummaryDto> GetDashboardRiskSummaryAsync(int userId, bool isAdmin);

    Task<PagedResult<TaskRiskDto>> GetTaskRisksAsync(
        int userId,
        bool isAdmin,
        string? search,
        int? projectId,
        int? engineerId,
        string? priority,
        string? status,
        string? riskLevel,
        string? sortBy,
        bool descending,
        int pageNumber,
        int pageSize);

    Task<PagedResult<ProjectRiskDto>> GetProjectRisksAsync(
        int userId,
        bool isAdmin,
        string? search,
        string? riskLevel,
        string? sortBy,
        bool descending,
        int pageNumber,
        int pageSize);
}
