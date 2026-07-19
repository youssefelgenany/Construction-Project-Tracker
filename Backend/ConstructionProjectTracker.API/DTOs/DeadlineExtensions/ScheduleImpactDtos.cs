using System.ComponentModel.DataAnnotations;

namespace ConstructionProjectTracker.API.DTOs.DeadlineExtensions;

public class AnalyzeTaskDeadlineExtensionDto
{
    [Required]
    public DateTime NewDueDate { get; set; }

    [Required]
    [MinLength(10)]
    [MaxLength(2000)]
    public string Reason { get; set; } = string.Empty;
}

public class ApplyTaskDeadlineExtensionDto
{
    [Required]
    public DateTime NewDueDate { get; set; }

    [Required]
    [MinLength(10)]
    [MaxLength(2000)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>Required when the analysis indicates the project end date must move.</summary>
    public bool ConfirmProjectExtension { get; set; }
}

public class ScheduleImpactTaskDto
{
    public int TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CurrentStart { get; set; }
    public DateTime CurrentDue { get; set; }
    public DateTime NewStart { get; set; }
    public DateTime NewDue { get; set; }
    public int DaysShifted { get; set; }
    public int? AssignedEngineerUserId { get; set; }
    public string? EngineerName { get; set; }
}

public class ScheduleImpactAnalysisDto
{
    public int SourceTaskId { get; set; }
    public string SourceTaskTitle { get; set; } = string.Empty;
    public DateTime CurrentDueDate { get; set; }
    public DateTime ProposedDueDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool HasConflicts { get; set; }
    public IReadOnlyList<ScheduleImpactTaskDto> AffectedTasks { get; set; } = [];
    public int AffectedTaskCount { get; set; }
    public int TotalShiftWorkingDays { get; set; }
    /// <summary>MAX(due date) across every project task after the in-memory simulation.</summary>
    public DateTime LatestTaskDueDate { get; set; }
    public DateTime CurrentProjectEnd { get; set; }
    /// <summary>Equals <see cref="LatestTaskDueDate"/> when a project extension is required; otherwise the current project end.</summary>
    public DateTime NewRequiredProjectEnd { get; set; }
    public bool RequiresProjectExtension { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
}

public class ApplyTaskDeadlineExtensionResultDto
{
    public string Message { get; set; } = string.Empty;
    public int ShiftedTaskCount { get; set; }
    public bool ProjectExtended { get; set; }
    public DateTime? NewProjectEndDate { get; set; }
}

/// <summary>
/// In-memory outcome of a staged cascade apply, used when the caller owns the transaction.
/// </summary>
public class StagedTaskDeadlineExtensionDto
{
    public ApplyTaskDeadlineExtensionResultDto Result { get; set; } = new();
    public int AdminUserId { get; set; }
    public int SourceTaskId { get; set; }
    public string SourceTaskTitle { get; set; } = string.Empty;
    public int? SourceEngineerUserId { get; set; }
    public DateTime PreviousSourceDue { get; set; }
    public DateTime NewSourceDue { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public bool ProjectExtended { get; set; }
    public DateTime? NewProjectEndDate { get; set; }
    public IReadOnlyList<ScheduleImpactTaskDto> AffectedTasks { get; set; } = [];
}

