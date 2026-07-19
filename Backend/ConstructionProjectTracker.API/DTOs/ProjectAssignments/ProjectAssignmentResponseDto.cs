namespace ConstructionProjectTracker.API.DTOs.ProjectAssignments;

public class ProjectAssignmentResponseDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int EngineerId { get; set; }
    public string EngineerName { get; set; } = string.Empty;
    public DateTime AssignedDate { get; set; }
}
