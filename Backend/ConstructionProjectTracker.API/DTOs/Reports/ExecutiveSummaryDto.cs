namespace ConstructionProjectTracker.API.DTOs.Reports;

public class ExecutiveSummaryDto
{
    public int TotalProjects { get; set; }
    public int HealthyProjects { get; set; }
    public int AtRiskProjects { get; set; }
    public int DelayedProjects { get; set; }
    public int TotalEngineers { get; set; }
    public int ActiveEngineers { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int OverdueTasks { get; set; }
    public double AverageProjectCompletion { get; set; }
    public double OnTimeCompletionRate { get; set; }
    public double AverageEngineerWorkload { get; set; }
}
