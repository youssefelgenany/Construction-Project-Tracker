using ConstructionProjectTracker.API.Enums;

namespace ConstructionProjectTracker.API.DTOs.Engineers;

public class EngineerWorkloadDto
{
    public int EngineerId { get; set; }
    public string EngineerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public bool IsActive { get; set; }
    public int TotalAssignedProjects { get; set; }
    public int ActiveProjects { get; set; }
    public int TotalAssignedTasks { get; set; }
    public int ActiveTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int PendingReviewTasks { get; set; }
    public int OverdueTasks { get; set; }
    public double AverageProgress { get; set; }
    public DateTime? EarliestUpcomingDeadline { get; set; }
    public WorkloadLevel WorkloadLevel { get; set; }

    /// <summary>Legacy alias used by dashboard and reports charts.</summary>
    public int AssignedProjects => TotalAssignedProjects;

    /// <summary>Legacy alias used by dashboard and reports charts.</summary>
    public int AssignedTasks => TotalAssignedTasks;

    /// <summary>Legacy alias: non-completed tasks.</summary>
    public int PendingTasks => ActiveTasks;

    /// <summary>Legacy alias: completion percentage across all assigned tasks.</summary>
    public double CompletionRate => TotalAssignedTasks > 0
        ? Math.Round((double)CompletedTasks / TotalAssignedTasks * 100, 2)
        : 0;
}
