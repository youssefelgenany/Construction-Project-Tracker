namespace ConstructionProjectTracker.API.DTOs.Risks;

public class DashboardRiskSummaryDto
{
    public int ProjectsAtRiskCount { get; set; }
    public int TasksAtRiskCount { get; set; }
    public int OverdueTasksCount { get; set; }
    public int PendingReviewsCount { get; set; }
    public IReadOnlyCollection<ProjectRiskDto> Projects { get; set; } = Array.Empty<ProjectRiskDto>();
}
