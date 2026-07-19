using ConstructionProjectTracker.API.DTOs.Engineers;

namespace ConstructionProjectTracker.API.DTOs.ProjectAssignments;

public class ProjectEngineersDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public List<EngineerResponseDto> Engineers { get; set; } = new();
}
