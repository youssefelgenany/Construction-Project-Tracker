namespace ConstructionProjectTracker.API.Entities;

public class TaskProgressLog
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int EngineerId { get; set; }
    public int PreviousProgress { get; set; }
    public int NewProgress { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TaskItem Task { get; set; } = null!;
    public Engineer Engineer { get; set; } = null!;
}
