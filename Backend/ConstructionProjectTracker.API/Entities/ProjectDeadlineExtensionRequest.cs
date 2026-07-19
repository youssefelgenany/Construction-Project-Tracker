using ConstructionProjectTracker.API.Enums;

namespace ConstructionProjectTracker.API.Entities;

public class ProjectDeadlineExtensionRequest
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int RequestedByUserId { get; set; }
    public DateTime CurrentEndDate { get; set; }
    public DateTime RequestedEndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ExtensionRequestStatus Status { get; set; } = ExtensionRequestStatus.Pending;
    public string? AdminComment { get; set; }
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Project Project { get; set; } = null!;
    public User RequestedByUser { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
}
