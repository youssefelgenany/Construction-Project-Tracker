using ConstructionProjectTracker.API.DTOs.Dashboard;
using ConstructionProjectTracker.API.DTOs.Tasks;

namespace ConstructionProjectTracker.API.Interfaces;

public interface ITaskScheduleService
{
    Task<ProjectTimelineDto?> GetProjectTimelineAsync(int projectId, int userId, bool isAdmin);

    Task<IReadOnlyList<CriticalPathTaskDto>> GetCriticalPathAsync(int projectId, int userId, bool isAdmin);

    Task<ScheduleSummaryDto> GetScheduleSummaryAsync(int userId, bool isAdmin);

    Task RefreshProjectBlockingStatesAsync(int projectId);
}
