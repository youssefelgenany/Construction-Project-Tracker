namespace ConstructionProjectTracker.API.DTOs.Reports;

public class TasksByStatusDto
{
    public int NotStarted { get; set; }
    public int InProgress { get; set; }
    public int Completed { get; set; }
}
