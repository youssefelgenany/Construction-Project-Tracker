using ConstructionProjectTracker.API.Enums;

namespace ConstructionProjectTracker.API.DTOs.Tasks;

public class TaskPrerequisiteDto
{
    public int TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public ConstructionProjectTracker.API.Enums.TaskStatus Status { get; set; }
    public bool IsComplete { get; set; }
}
