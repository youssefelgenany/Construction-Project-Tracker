namespace ConstructionProjectTracker.API.Entities;

public class TaskCompletionApprovalHistory
{
    public int Id { get; set; }
    public int TaskCompletionReportId { get; set; }
    public string Action { get; set; } = string.Empty;
    public int ReviewedByUserId { get; set; }
    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
    public string? RejectionReason { get; set; }

    public TaskCompletionReport TaskCompletionReport { get; set; } = null!;
    public User ReviewedByUser { get; set; } = null!;
}
