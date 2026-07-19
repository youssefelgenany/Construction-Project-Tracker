using ConstructionProjectTracker.API.Enums;

namespace ConstructionProjectTracker.API.Entities;

public class TaskDeadlineExtensionRequest
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int RequestedByUserId { get; set; }
    public DateTime CurrentDueDate { get; set; }
    public DateTime RequestedDueDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ExtensionRequestStatus Status { get; set; } = ExtensionRequestStatus.Pending;
    public string? AdminComment { get; set; }
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TaskItem Task { get; set; } = null!;
    public User RequestedByUser { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
}
