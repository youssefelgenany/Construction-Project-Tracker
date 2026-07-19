using ConstructionProjectTracker.API.DTOs.Engineers;
using ConstructionProjectTracker.API.DTOs.Projects;
using ConstructionProjectTracker.API.Enums;

namespace ConstructionProjectTracker.API.DTOs.Tasks;

public class TaskDetailsDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int? AssignedEngineerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; }
    public int CompletionPercentage { get; set; }
    public ConstructionProjectTracker.API.Enums.TaskStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime UpdatedAt { get; set; }
    public EngineerResponseDto? AssignedEngineer { get; set; }
    public ProjectResponseDto Project { get; set; } = null!;
    public TaskCompletionReportDto? CompletionReport { get; set; }
    public IReadOnlyList<TaskDependencyDto> Dependencies { get; set; } = Array.Empty<TaskDependencyDto>();
    public IReadOnlyList<TaskPrerequisiteDto> IncompletePrerequisites { get; set; } = Array.Empty<TaskPrerequisiteDto>();
}
