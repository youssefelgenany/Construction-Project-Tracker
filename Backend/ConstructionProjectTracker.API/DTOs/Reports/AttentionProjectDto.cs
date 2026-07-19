using ConstructionProjectTracker.API.Enums;

namespace ConstructionProjectTracker.API.DTOs.Reports;

public class AttentionProjectDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; }
    public int CompletionPercent { get; set; }
    public int OverdueTasks { get; set; }
    public List<string> AssignedEngineers { get; set; } = [];
    public string Reason { get; set; } = string.Empty;
    public PredictionState? PredictionState { get; set; }
}
