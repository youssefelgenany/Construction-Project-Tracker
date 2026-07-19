namespace ConstructionProjectTracker.API.DTOs.Reports;

public class ReportsSummaryDto
{
    public int TotalProjects { get; set; }
    public int ActiveProjects { get; set; }
    public int CompletedProjects { get; set; }
    public int DelayedProjects { get; set; }
    public int TotalEngineers { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int OverdueTasks { get; set; }
    public double AverageProjectProgress { get; set; }
    public int TotalDocuments { get; set; }
}
