namespace ConstructionProjectTracker.API.Entities;

public class ProjectDeadlineHistory
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public DateTime PreviousEndDate { get; set; }
    public DateTime NewEndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public bool IsAutomatic { get; set; }

    public Project Project { get; set; } = null!;
    public User ChangedByUser { get; set; } = null!;
}
