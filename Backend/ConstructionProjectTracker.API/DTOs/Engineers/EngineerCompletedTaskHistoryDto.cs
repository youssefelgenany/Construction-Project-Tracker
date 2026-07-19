namespace ConstructionProjectTracker.API.DTOs.Engineers;

public class EngineerCompletedTaskHistoryDto
{
    public int TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
    public DateTime DueDate { get; set; }
    public bool FinishedOnTime { get; set; }
    public int DaysEarlyLate { get; set; }
    public double DurationDays { get; set; }
    public int ProgressUpdates { get; set; }
}
