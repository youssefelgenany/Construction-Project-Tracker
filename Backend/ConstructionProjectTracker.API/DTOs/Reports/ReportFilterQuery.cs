using ConstructionProjectTracker.API.Enums;

namespace ConstructionProjectTracker.API.DTOs.Reports;

public class ReportFilterQuery
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? ProjectId { get; set; }
    public int? EngineerId { get; set; }
    public ProjectStatus? Status { get; set; }
    public RiskLevel? RiskLevel { get; set; }
}
