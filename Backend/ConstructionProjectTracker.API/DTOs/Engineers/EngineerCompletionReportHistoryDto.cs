namespace ConstructionProjectTracker.API.DTOs.Engineers;

public class EngineerCompletionReportHistoryDto
{
    public int ReportId { get; set; }
    public int TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string ReviewStatus { get; set; } = string.Empty;
}
