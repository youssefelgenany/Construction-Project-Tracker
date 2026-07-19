using ConstructionProjectTracker.API.Enums;

namespace ConstructionProjectTracker.API.DTOs.Risks;

public class ProjectRiskDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Budget { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ProjectStatus Status { get; set; }
    public int ProgressPercentage { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
    public int ActiveTaskCount { get; set; }
    public int AtRiskTaskCount { get; set; }
    public int OverdueTaskCount { get; set; }
    public bool HasCriticalOverdueTask { get; set; }
}
