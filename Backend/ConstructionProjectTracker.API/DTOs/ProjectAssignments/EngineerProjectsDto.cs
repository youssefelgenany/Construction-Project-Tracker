using ConstructionProjectTracker.API.DTOs.Projects;

namespace ConstructionProjectTracker.API.DTOs.ProjectAssignments;

public class EngineerProjectsDto
{
    public int EngineerId { get; set; }
    public string EngineerName { get; set; } = string.Empty;
    public List<ProjectResponseDto> Projects { get; set; } = new();
}
