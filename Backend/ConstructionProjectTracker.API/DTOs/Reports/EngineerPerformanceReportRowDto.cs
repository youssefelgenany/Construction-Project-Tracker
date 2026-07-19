using ConstructionProjectTracker.API.Enums;

namespace ConstructionProjectTracker.API.DTOs.Reports;

public class EngineerPerformanceReportRowDto
{
    public int EngineerId { get; set; }
    public string EngineerName { get; set; } = string.Empty;
    public int Projects { get; set; }
    public int CompletedTasks { get; set; }
    public double AverageCompletionPercent { get; set; }
    public double OnTimeRate { get; set; }
    public int OverdueTasks { get; set; }
    public double CurrentWorkloadPercent { get; set; }
    public WorkloadLevel CurrentWorkloadLevel { get; set; }
    public double AverageDelayDays { get; set; }
    public double PerformanceScore { get; set; }
    public bool IsTopPerformer { get; set; }
}
