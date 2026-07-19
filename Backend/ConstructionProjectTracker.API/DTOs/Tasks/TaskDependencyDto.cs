namespace ConstructionProjectTracker.API.DTOs.Tasks;

public class TaskDependencyDto
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int DependsOnTaskId { get; set; }
    public string DependsOnTaskTitle { get; set; } = string.Empty;
}
