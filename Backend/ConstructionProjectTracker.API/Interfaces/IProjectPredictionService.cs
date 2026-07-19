using ConstructionProjectTracker.API.DTOs.Predictions;
using ConstructionProjectTracker.API.Entities;

namespace ConstructionProjectTracker.API.Interfaces;

/// <summary>
/// Lifecycle-aware construction forecasting engine (schedule phase → velocity phase).
/// Future risk / recovery / recommendation features should compose this service.
/// </summary>
public interface IProjectPredictionService
{
    ProjectDelayPredictionDto CalculatePrediction(Project project, DateTime? asOfDate = null);

    ProjectDelayPredictionDto CalculatePrediction(
        Project project,
        IReadOnlyList<int> taskCompletionPercentages,
        DateTime? asOfDate = null);

    Task<ProjectDelayPredictionDto?> GetProjectPredictionAsync(
        int projectId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectDelayPredictionDto>> GetActiveProjectPredictionsAsync(
        CancellationToken cancellationToken = default);
}
