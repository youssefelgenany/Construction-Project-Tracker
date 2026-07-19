namespace ConstructionProjectTracker.API.Entities;

public class TaskDeadlineHistory
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public DateTime? PreviousStartDate { get; set; }
    public DateTime? NewStartDate { get; set; }
    public DateTime PreviousDueDate { get; set; }
    public DateTime NewDueDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public bool IsAutomatic { get; set; }

    public TaskItem Task { get; set; } = null!;
    public User ChangedByUser { get; set; } = null!;
}
