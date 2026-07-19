namespace ConstructionProjectTracker.API.Entities;

public class TaskDependency
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int DependsOnTaskId { get; set; }

    public TaskItem Task { get; set; } = null!;
    public TaskItem DependsOnTask { get; set; } = null!;
}
