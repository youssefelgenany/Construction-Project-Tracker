namespace ConstructionProjectTracker.API.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PerformedByUserId { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }

    public User PerformedByUser { get; set; } = null!;
}
