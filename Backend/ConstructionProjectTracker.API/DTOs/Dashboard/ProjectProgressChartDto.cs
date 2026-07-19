using ConstructionProjectTracker.API.Enums;

namespace ConstructionProjectTracker.API.DTOs.Dashboard;

public class ProjectProgressChartDto
{
    public string ProjectName { get; set; } = string.Empty;
    public int ProgressPercentage { get; set; }
    public ProjectStatus Status { get; set; }
}
