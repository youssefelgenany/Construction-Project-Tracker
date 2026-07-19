namespace ConstructionProjectTracker.API.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public int TotalProjects { get; set; }
    public int ActiveProjects { get; set; }
    public int CompletedProjects { get; set; }
    public int NotStartedProjects { get; set; }
    public int TotalEngineers { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int PendingTasks { get; set; }
    public double AverageProjectProgress { get; set; }
    public int TotalDocuments { get; set; }
}
