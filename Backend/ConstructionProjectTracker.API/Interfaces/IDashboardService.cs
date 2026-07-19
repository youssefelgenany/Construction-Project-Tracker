using ConstructionProjectTracker.API.DTOs.Dashboard;
using ConstructionProjectTracker.API.DTOs.Engineers;

namespace ConstructionProjectTracker.API.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(int userId, bool isAdmin);

    Task<IEnumerable<ProjectProgressChartDto>> GetProjectProgressAsync(int userId, bool isAdmin);

    Task<IEnumerable<EngineerWorkloadDto>> GetEngineerWorkloadAsync(int userId, bool isAdmin);

    Task<IEnumerable<EngineerPerformanceDto>> GetTopPerformingEngineersAsync(int userId, bool isAdmin);

    Task<ProjectStatusDistributionDto> GetProjectStatusDistributionAsync();

    Task<IEnumerable<MonthlyProjectsDto>> GetMonthlyProjectsAsync();

    Task<IEnumerable<RecentActivityDto>> GetRecentActivitiesAsync();
}
