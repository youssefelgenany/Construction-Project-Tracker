using ConstructionProjectTracker.API.DTOs.DeadlineExtensions;

namespace ConstructionProjectTracker.API.Interfaces;

public interface ITaskDeadlineCascadeService
{
    Task<ScheduleImpactAnalysisDto> AnalyzeAsync(int taskId, AnalyzeTaskDeadlineExtensionDto dto);

    /// <summary>
    /// Applies the extension (and optional project end update) in its own transaction, then notifies.
    /// </summary>
    Task<ApplyTaskDeadlineExtensionResultDto> ApplyAsync(
        int taskId,
        ApplyTaskDeadlineExtensionDto dto,
        int adminUserId);

    /// <summary>
    /// Stages all schedule changes on the current DbContext without SaveChanges or notifications.
    /// Caller must persist and notify inside its own transaction.
    /// </summary>
    Task<StagedTaskDeadlineExtensionDto> StageApplyAsync(
        int taskId,
        ApplyTaskDeadlineExtensionDto dto,
        int adminUserId);

    Task NotifyStagedApplyAsync(StagedTaskDeadlineExtensionDto staged);
}
