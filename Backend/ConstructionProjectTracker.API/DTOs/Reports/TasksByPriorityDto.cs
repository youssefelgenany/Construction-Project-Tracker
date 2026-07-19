namespace ConstructionProjectTracker.API.DTOs.Reports;

public class TasksByPriorityDto
{
    public int Low { get; set; }
    public int Medium { get; set; }
    public int High { get; set; }
    public int Critical { get; set; }
}
