using ConstructionProjectTracker.API.Enums;

namespace ConstructionProjectTracker.API.DTOs.Predictions;

public class ProjectDelayPredictionDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public PredictionState PredictionState { get; set; }
    public double ExpectedProgress { get; set; }
    public double CurrentProgress { get; set; }
    public double Velocity { get; set; }
    public int TotalWorkingDays { get; set; }
    public int ElapsedWorkingDays { get; set; }
    public int RemainingWorkingDays { get; set; }
    public double RemainingWork { get; set; }
    public double EstimatedRemainingWorkingDays { get; set; }
    public DateTime? PredictedFinishDate { get; set; }
    public int DelayWorkingDays { get; set; }
    public PredictionConfidenceLevel ConfidenceLevel { get; set; }
    public string RiskMessage { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;

    /// <summary>True when the project is forecasted or already late (AtRisk / Delayed / DelayedStart).</summary>
    public bool WillMissDeadline =>
        PredictionState is PredictionState.AtRisk
            or PredictionState.Delayed
            or PredictionState.DelayedStart;
}
