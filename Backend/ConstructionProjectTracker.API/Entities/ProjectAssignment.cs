namespace ConstructionProjectTracker.API.Entities;

public class ProjectAssignment
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int EngineerId { get; set; }
    public DateTime AssignedDate { get; set; }

    public Project Project { get; set; } = null!;
    public Engineer Engineer { get; set; } = null!;
}
