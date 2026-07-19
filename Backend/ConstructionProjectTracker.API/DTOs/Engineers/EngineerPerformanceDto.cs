using ConstructionProjectTracker.API.Enums;

namespace ConstructionProjectTracker.API.DTOs.Engineers;

public class EngineerPerformanceDto
{
    public int EngineerId { get; set; }
    public string EngineerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int TotalProjectsWorkedOn { get; set; }
    public int ProjectsCompleted { get; set; }
    public int TotalTasksAssigned { get; set; }
    public int TotalTasksCompleted { get; set; }
    public double CompletionRate { get; set; }
    public int TasksFinishedBeforeDeadline { get; set; }
    public int TasksFinishedLate { get; set; }
    public double OnTimeCompletionRate { get; set; }
    public double LateRate { get; set; }
    public double AverageDaysEarlyLate { get; set; }
    public double AverageTaskDuration { get; set; }
    public double AverageProgressUpdatesPerTask { get; set; }
    public int TotalCompletionReportsSubmitted { get; set; }
    public int CurrentActiveTasks { get; set; }
    public double PerformanceScore { get; set; }
    public PerformanceTier PerformanceTier { get; set; }
}
