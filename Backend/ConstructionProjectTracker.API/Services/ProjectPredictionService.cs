using ConstructionProjectTracker.API.Data;
using ConstructionProjectTracker.API.DTOs.Predictions;
using ConstructionProjectTracker.API.Entities;
using ConstructionProjectTracker.API.Enums;
using ConstructionProjectTracker.API.Helpers;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConstructionProjectTracker.API.Services;

public class ProjectPredictionService : IProjectPredictionService
{
    private readonly ApplicationDbContext _context;

    public ProjectPredictionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public ProjectDelayPredictionDto CalculatePrediction(Project project, DateTime? asOfDate = null)
    {
        var taskProgress = project.Tasks?.Select(t => t.CompletionPercentage).ToList()
            ?? [];
        return CalculatePrediction(project, taskProgress, asOfDate);
    }

    public ProjectDelayPredictionDto CalculatePrediction(
        Project project,
        IReadOnlyList<int> taskCompletionPercentages,
        DateTime? asOfDate = null)
    {
        var today = (asOfDate ?? DateTime.UtcNow).Date;
        var start = project.StartDate.Date;
        var end = project.EndDate.Date;
        var taskCount = taskCompletionPercentages.Count;
        var currentProgress = CalculateProgressFromTasks(taskCompletionPercentages);

        var totalWorkingDays = Math.Max(1, WorkingDaysCalculator.CountTotalWorkingDays(start, end));
        var elapsedWorkingDays = today <= start
            ? 0
            : WorkingDaysCalculator.CountWorkingDays(start, today);
        var remainingWorkingDays = WorkingDaysCalculator.CountRemainingWorkingDays(today, end);

        var dto = new ProjectDelayPredictionDto
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            ExpectedProgress = 0,
            CurrentProgress = currentProgress,
            TotalWorkingDays = totalWorkingDays,
            ElapsedWorkingDays = elapsedWorkingDays,
            RemainingWorkingDays = remainingWorkingDays,
            RemainingWork = 0,
            Velocity = 0,
            EstimatedRemainingWorkingDays = 0,
            PredictedFinishDate = null,
            DelayWorkingDays = 0,
            ConfidenceLevel = PredictionConfidenceLevel.None
        };

        // Projects with no tasks are waiting for planning — never delayed or at risk.
        if (taskCount == 0)
        {
            return ApplyState(
                dto,
                PredictionState.WaitingForPlanning,
                statusLabel: "Waiting for Planning",
                delay: 0,
                riskMessage: "This project does not yet contain any scheduled tasks.",
                predictedFinishDate: null);
        }

        var expectedProgress = Math.Clamp(
            Math.Round(elapsedWorkingDays / (double)totalWorkingDays * 100, 2, MidpointRounding.AwayFromZero),
            0,
            100);
        dto.ExpectedProgress = expectedProgress;
        dto.RemainingWork = Math.Round(Math.Max(0, 100 - currentProgress), 2, MidpointRounding.AwayFromZero);

        // Stage A — project has not reached its planned start.
        if (today < start)
        {
            return ApplyState(
                dto,
                PredictionState.Scheduled,
                statusLabel: "Scheduled",
                delay: 0,
                riskMessage: "Project has not started yet.",
                predictedFinishDate: null);
        }

        // Stage — actual delay (past planned end, still incomplete). Takes priority over forecast.
        if (today > end && currentProgress < 100)
        {
            var actualDelay = WorkingDaysCalculator.CountWorkingDaysBetween(end, today);
            return ApplyState(
                dto,
                PredictionState.Delayed,
                statusLabel: "Delayed",
                delay: actualDelay,
                riskMessage: $"Planned end date has passed. Actual delay is {actualDelay} working day(s).",
                predictedFinishDate: null);
        }

        // Stage B — start date reached but no work recorded.
        if (currentProgress <= 0)
        {
            if (elapsedWorkingDays == 0)
            {
                return ApplyState(
                    dto,
                    PredictionState.Scheduled,
                    statusLabel: "Project starts today",
                    delay: 0,
                    riskMessage: "Project starts today. No delay yet.",
                    predictedFinishDate: end);
            }

            return ApplyState(
                dto,
                PredictionState.DelayedStart,
                statusLabel: "Project has not started",
                delay: elapsedWorkingDays,
                riskMessage:
                    $"Planned start has passed with 0% progress. {elapsedWorkingDays} scheduled working day(s) consumed without work.",
                predictedFinishDate: WorkingDaysCalculator.AddWorkingDays(end, elapsedWorkingDays));
        }

        // Completed work — on track by definition.
        if (currentProgress >= 100)
        {
            return ApplyState(
                dto,
                PredictionState.OnTrack,
                statusLabel: "On Track",
                delay: 0,
                riskMessage: "Project work is complete.",
                predictedFinishDate: today,
                confidence: ResolveConfidence(elapsedWorkingDays));
        }

        // Stage C — velocity forecast.
        var elapsedForVelocity = Math.Max(elapsedWorkingDays, 1);
        var velocity = Math.Round(currentProgress / elapsedForVelocity, 2, MidpointRounding.AwayFromZero);
        dto.Velocity = velocity;

        var estimatedRemaining = Math.Round(dto.RemainingWork / velocity, 2, MidpointRounding.AwayFromZero);
        dto.EstimatedRemainingWorkingDays = estimatedRemaining;
        var predictedFinish = WorkingDaysCalculator.AddWorkingDays(today, estimatedRemaining);
        var confidence = ResolveConfidence(elapsedWorkingDays);

        if (estimatedRemaining <= remainingWorkingDays)
        {
            return ApplyState(
                dto,
                PredictionState.OnTrack,
                statusLabel: "On Track",
                delay: 0,
                riskMessage: "Current execution speed is sufficient to finish on time.",
                predictedFinishDate: predictedFinish,
                confidence: confidence);
        }

        var delay = (int)Math.Round(estimatedRemaining - remainingWorkingDays, MidpointRounding.AwayFromZero);
        delay = Math.Max(delay, 1);

        return ApplyState(
            dto,
            PredictionState.AtRisk,
            statusLabel: "At Risk",
            delay: delay,
            riskMessage:
                $"Current velocity of {velocity:0.##}% per working day forecasts a late finish by {delay} working day(s).",
            predictedFinishDate: predictedFinish,
            confidence: confidence);
    }

    public async Task<ProjectDelayPredictionDto?> GetProjectPredictionAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        return project is null ? null : CalculatePrediction(project);
    }

    public async Task<IReadOnlyList<ProjectDelayPredictionDto>> GetActiveProjectPredictionsAsync(
        CancellationToken cancellationToken = default)
    {
        var projects = await _context.Projects
            .AsNoTracking()
            .Include(p => p.Tasks)
            .Where(p => p.Status != ProjectStatus.Completed)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return projects.Select(p => CalculatePrediction(p)).ToList();
    }

    private static ProjectDelayPredictionDto ApplyState(
        ProjectDelayPredictionDto dto,
        PredictionState state,
        string statusLabel,
        int delay,
        string riskMessage,
        DateTime? predictedFinishDate,
        PredictionConfidenceLevel confidence = PredictionConfidenceLevel.None)
    {
        dto.PredictionState = state;
        dto.StatusLabel = statusLabel;
        dto.DelayWorkingDays = delay;
        dto.RiskMessage = riskMessage;
        dto.PredictedFinishDate = predictedFinishDate;
        dto.ConfidenceLevel = confidence;
        return dto;
    }

    private static PredictionConfidenceLevel ResolveConfidence(int elapsedWorkingDays)
    {
        if (elapsedWorkingDays >= 11)
        {
            return PredictionConfidenceLevel.High;
        }

        if (elapsedWorkingDays >= 6)
        {
            return PredictionConfidenceLevel.Medium;
        }

        if (elapsedWorkingDays >= 3)
        {
            return PredictionConfidenceLevel.Low;
        }

        // Progress exists but sample is thinner than the Low band — still treat as Low.
        return elapsedWorkingDays > 0
            ? PredictionConfidenceLevel.Low
            : PredictionConfidenceLevel.None;
    }

    private static double CalculateProgressFromTasks(IReadOnlyList<int> taskCompletionPercentages)
    {
        if (taskCompletionPercentages.Count == 0)
        {
            return 0;
        }

        return Math.Round(
            taskCompletionPercentages.Average(),
            2,
            MidpointRounding.AwayFromZero);
    }
}
