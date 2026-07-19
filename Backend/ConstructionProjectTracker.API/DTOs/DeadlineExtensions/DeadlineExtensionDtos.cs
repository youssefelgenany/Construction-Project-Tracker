using ConstructionProjectTracker.API.Enums;
using System.ComponentModel.DataAnnotations;

namespace ConstructionProjectTracker.API.DTOs.DeadlineExtensions;

public class CreateTaskDeadlineExtensionRequestDto
{
    [Required]
    public DateTime RequestedDueDate { get; set; }

    [Required]
    [MinLength(20)]
    [MaxLength(2000)]
    public string Reason { get; set; } = string.Empty;
}

public class CreateProjectDeadlineExtensionRequestDto
{
    [Required]
    public DateTime RequestedEndDate { get; set; }

    [Required]
    [MinLength(20)]
    [MaxLength(2000)]
    public string Reason { get; set; } = string.Empty;
}

public class AdminExtendTaskDeadlineDto
{
    [Required]
    public DateTime NewDueDate { get; set; }

    [Required]
    [MinLength(10)]
    [MaxLength(2000)]
    public string Reason { get; set; } = string.Empty;
}

public class AdminExtendProjectDeadlineDto
{
    [Required]
    public DateTime NewEndDate { get; set; }

    [Required]
    [MinLength(10)]
    [MaxLength(2000)]
    public string Reason { get; set; } = string.Empty;
}

public class ReviewDeadlineExtensionDto
{
    [MaxLength(2000)]
    public string? AdminComment { get; set; }

    /// <summary>Required when approving a task request that also extends the project end date.</summary>
    public bool ConfirmProjectExtension { get; set; }
}

public class DeadlineExtensionRequestDto
{
    public int Id { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public int? TaskId { get; set; }
    public string? TaskTitle { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int RequestedByUserId { get; set; }
    public string EngineerName { get; set; } = string.Empty;
    public DateTime CurrentDeadline { get; set; }
    public DateTime RequestedDeadline { get; set; }
    public int RequestedExtraDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ExtensionRequestStatus Status { get; set; }
    public string? AdminComment { get; set; }
    public int? ReviewedByUserId { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TaskDeadlineHistoryDto
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public DateTime? PreviousStartDate { get; set; }
    public DateTime? NewStartDate { get; set; }
    public DateTime PreviousDueDate { get; set; }
    public DateTime NewDueDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int ChangedByUserId { get; set; }
    public string ChangedByName { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public bool IsAutomatic { get; set; }
}

public class ProjectDeadlineHistoryDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public DateTime PreviousEndDate { get; set; }
    public DateTime NewEndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int ChangedByUserId { get; set; }
    public string ChangedByName { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public bool IsAutomatic { get; set; }
}
