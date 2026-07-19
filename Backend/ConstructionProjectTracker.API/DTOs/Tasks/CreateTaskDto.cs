using ConstructionProjectTracker.API.Enums;

namespace ConstructionProjectTracker.API.DTOs.Tasks;

public class CreateTaskDto
{
    public int ProjectId { get; set; }
    public int AssignedEngineerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
}
