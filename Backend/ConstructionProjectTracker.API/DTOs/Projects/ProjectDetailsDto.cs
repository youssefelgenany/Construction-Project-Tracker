using ConstructionProjectTracker.API.Enums;

namespace ConstructionProjectTracker.API.DTOs.Projects;

public class ProjectDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Budget { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ProjectStatus Status { get; set; }
    public int ProgressPercentage { get; set; }
    public int AssignedEngineersCount { get; set; }
    public int TasksCount { get; set; }
    public int DocumentsCount { get; set; }
}
