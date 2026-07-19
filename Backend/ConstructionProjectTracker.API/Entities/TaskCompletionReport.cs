namespace ConstructionProjectTracker.API.Entities;

public class TaskCompletionReport
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int UploadedByUserId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string RelativeFilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string? RejectionComment { get; set; }
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public TaskItem Task { get; set; } = null!;
    public User UploadedByUser { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
    public ICollection<TaskCompletionApprovalHistory> ApprovalHistory { get; set; } = new List<TaskCompletionApprovalHistory>();
}
