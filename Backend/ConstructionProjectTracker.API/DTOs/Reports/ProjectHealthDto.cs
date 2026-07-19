namespace ConstructionProjectTracker.API.DTOs.Reports;

public class ProjectHealthDto
{
    public int Healthy { get; set; }
    public int AtRisk { get; set; }
    public int Critical { get; set; }
    public int Completed { get; set; }
    public int Total => Healthy + AtRisk + Critical + Completed;
}
