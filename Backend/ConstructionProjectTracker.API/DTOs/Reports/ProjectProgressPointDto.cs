namespace ConstructionProjectTracker.API.DTOs.Reports;

public class ProjectProgressPointDto
{
    public string Label { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public double AverageCompletionPercent { get; set; }
}
