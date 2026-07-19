namespace ConstructionProjectTracker.API.DTOs.Tasks;

public class ProjectTimelineDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateTime ProjectStartDate { get; set; }
    public DateTime ProjectEndDate { get; set; }
    public IReadOnlyList<TimelineTaskDto> Tasks { get; set; } = Array.Empty<TimelineTaskDto>();
    public IReadOnlyList<TaskDependencyDto> Dependencies { get; set; } = Array.Empty<TaskDependencyDto>();
}
