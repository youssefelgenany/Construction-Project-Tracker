using ConstructionProjectTracker.API.Enums;

namespace ConstructionProjectTracker.API.DTOs.Tasks;

public class TimelineTaskDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public int CompletionPercentage { get; set; }
    public ConstructionProjectTracker.API.Enums.TaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public string? EngineerName { get; set; }
    public bool IsOverdue { get; set; }
    public bool IsCritical { get; set; }
    public bool IsBlocked { get; set; }
    public IReadOnlyList<int> DependsOnTaskIds { get; set; } = Array.Empty<int>();
    public IReadOnlyList<TaskPrerequisiteDto> IncompletePrerequisites { get; set; } = Array.Empty<TaskPrerequisiteDto>();
}
