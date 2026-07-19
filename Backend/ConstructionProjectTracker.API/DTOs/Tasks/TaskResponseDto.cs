using ConstructionProjectTracker.API.Enums;

namespace ConstructionProjectTracker.API.DTOs.Tasks;

public class TaskResponseDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int? AssignedEngineerId { get; set; }
    public string? EngineerName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; }
    public int CompletionPercentage { get; set; }
    public ConstructionProjectTracker.API.Enums.TaskStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public TaskCompletionReportDto? CompletionReport { get; set; }
    public IReadOnlyList<TaskPrerequisiteDto> IncompletePrerequisites { get; set; } = Array.Empty<TaskPrerequisiteDto>();
    public int DependencyCount { get; set; }
}
