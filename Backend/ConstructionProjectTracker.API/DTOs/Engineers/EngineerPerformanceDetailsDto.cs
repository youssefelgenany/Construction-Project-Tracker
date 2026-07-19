namespace ConstructionProjectTracker.API.DTOs.Engineers;

public class EngineerPerformanceDetailsDto
{
    public EngineerPerformanceDto Summary { get; set; } = new();
    public IReadOnlyList<EngineerPerformanceTrendPointDto> Trend { get; set; } = Array.Empty<EngineerPerformanceTrendPointDto>();
    public IReadOnlyList<EngineerCompletedTaskHistoryDto> RecentCompletedTasks { get; set; } = Array.Empty<EngineerCompletedTaskHistoryDto>();
    public IReadOnlyList<EngineerCompletionReportHistoryDto> RecentCompletionReports { get; set; } = Array.Empty<EngineerCompletionReportHistoryDto>();
}
