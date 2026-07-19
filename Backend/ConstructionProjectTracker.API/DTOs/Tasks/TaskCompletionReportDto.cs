namespace ConstructionProjectTracker.API.DTOs.Tasks;

public class TaskCompletionReportDto
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = "Pending Review";
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? RejectionComment { get; set; }
    public List<TaskCompletionApprovalHistoryDto> ApprovalHistory { get; set; } = [];
}

public class TaskCompletionApprovalHistoryDto
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ReviewedBy { get; set; } = string.Empty;
    public DateTime ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
}

public class ValidPrerequisiteTaskDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public ConstructionProjectTracker.API.Enums.TaskStatus Status { get; set; }
}
