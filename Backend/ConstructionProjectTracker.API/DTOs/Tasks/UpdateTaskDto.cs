using ConstructionProjectTracker.API.Enums;

namespace ConstructionProjectTracker.API.DTOs.Tasks;

public class UpdateTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; }
    public int CompletionPercentage { get; set; }
    public ConstructionProjectTracker.API.Enums.TaskStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
}
