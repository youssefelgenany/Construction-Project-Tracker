namespace ConstructionProjectTracker.API.DTOs.Reports;

public class ReportProjectRowDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? Manager { get; set; }
    public int ProgressPercentage { get; set; }
    public int OpenTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int DocumentsCount { get; set; }
    public int EngineersAssigned { get; set; }
}
