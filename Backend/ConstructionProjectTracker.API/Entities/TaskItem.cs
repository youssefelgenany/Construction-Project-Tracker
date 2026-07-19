using ConstructionProjectTracker.API.Enums;

namespace ConstructionProjectTracker.API.Entities;

public class TaskItem
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int? AssignedEngineerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public int CompletionPercentage { get; set; }
    public ConstructionProjectTracker.API.Enums.TaskStatus Status { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Project Project { get; set; } = null!;
    public Engineer? AssignedEngineer { get; set; }
    public TaskCompletionReport? CompletionReport { get; set; }
    public ICollection<TaskProgressLog> ProgressLogs { get; set; } = new List<TaskProgressLog>();
    public ICollection<TaskDependency> Dependencies { get; set; } = new List<TaskDependency>();
    public ICollection<TaskDependency> DependentTasks { get; set; } = new List<TaskDependency>();
}
