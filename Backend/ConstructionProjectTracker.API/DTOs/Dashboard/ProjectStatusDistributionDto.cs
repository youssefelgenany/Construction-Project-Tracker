namespace ConstructionProjectTracker.API.DTOs.Dashboard;

public class ProjectStatusDistributionDto
{
    public int Completed { get; set; }
    public int InProgress { get; set; }
    public int NotStarted { get; set; }
}
