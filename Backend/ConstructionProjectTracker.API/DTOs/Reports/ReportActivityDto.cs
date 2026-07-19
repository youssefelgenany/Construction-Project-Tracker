namespace ConstructionProjectTracker.API.DTOs.Reports;

public class ReportActivityDto
{
    public DateTime Time { get; set; }
    public string User { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public string? TaskTitle { get; set; }
    public int? ProjectId { get; set; }
    public int? TaskId { get; set; }
}
