namespace ConstructionProjectTracker.API.DTOs.ProjectAssignments;

public class ProjectEngineerAssignmentDto
{
    public int Id { get; set; }
    public int EngineerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateTime AssignedDate { get; set; }
}
