using ConstructionProjectTracker.API.DTOs.Tasks;
using ConstructionProjectTracker.API.Enums;

namespace ConstructionProjectTracker.API.DTOs.Risks;

public class TaskRiskDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int? AssignedEngineerId { get; set; }
    public string? EngineerName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; }
    public int CompletionPercentage { get; set; }
    public ConstructionProjectTracker.API.Enums.TaskStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public TaskCompletionReportDto? CompletionReport { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
    public int DaysOverdue { get; set; }
}
