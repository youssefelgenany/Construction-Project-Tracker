namespace ConstructionProjectTracker.API.DTOs.Engineers;

public class EngineerPerformanceTrendPointDto
{
    public string Label { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public int CompletedTasks { get; set; }
    public double OnTimeRate { get; set; }
    public double PerformanceScore { get; set; }
}
