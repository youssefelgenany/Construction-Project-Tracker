namespace ConstructionProjectTracker.API.DTOs.Reports;

public class WorkloadBarDto
{
    public int EngineerId { get; set; }
    public string EngineerName { get; set; } = string.Empty;
    public int ActiveTasks { get; set; }
    public int OverdueTasks { get; set; }
    public double WorkloadPercent { get; set; }
}
