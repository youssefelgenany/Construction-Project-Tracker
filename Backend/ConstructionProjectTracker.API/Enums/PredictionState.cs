namespace ConstructionProjectTracker.API.Enums;

public enum PredictionState
{
    Scheduled = 0,
    DelayedStart = 1,
    OnTrack = 2,
    AtRisk = 3,
    Delayed = 4,
    WaitingForPlanning = 5
}

public enum PredictionConfidenceLevel
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3
}
