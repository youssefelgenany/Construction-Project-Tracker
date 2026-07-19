using System.Globalization;
using ClosedXML.Excel;
using ConstructionProjectTracker.API.Data;
using ConstructionProjectTracker.API.DTOs.Common;
using ConstructionProjectTracker.API.DTOs.Dashboard;
using ConstructionProjectTracker.API.DTOs.Engineers;
using ConstructionProjectTracker.API.DTOs.Reports;
using ConstructionProjectTracker.API.DTOs.Risks;
using ConstructionProjectTracker.API.Entities;
using ConstructionProjectTracker.API.Enums;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TaskStatus = ConstructionProjectTracker.API.Enums.TaskStatus;

namespace ConstructionProjectTracker.API.Services;

public class ReportsService : IReportsService
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;
    private const int SoftWorkloadCapacity = 15;
    private const int StaleProgressDays = 14;
    private const string CompanyName = "Construction Tracker";

    private readonly ApplicationDbContext _context;
    private readonly IRiskAnalysisService _riskAnalysisService;
    private readonly IProjectPredictionService _predictionService;
    private readonly ILogger<ReportsService> _logger;

    public ReportsService(
        ApplicationDbContext context,
        IRiskAnalysisService riskAnalysisService,
        IProjectPredictionService predictionService,
        ILogger<ReportsService> logger)
    {
        _context = context;
        _riskAnalysisService = riskAnalysisService;
        _predictionService = predictionService;
        _logger = logger;
    }

    public async Task<ExecutiveSummaryDto> GetExecutiveSummaryAsync(ReportFilterQuery filter)
    {
        var projects = ApplyProjectFilters(_context.Projects.AsNoTracking(), filter);
        var tasks = ApplyTaskFilters(_context.Tasks.AsNoTracking(), filter, projects);
        var today = DateTime.UtcNow.Date;

        var projectList = await projects
            .Select(p => new { p.Id, p.Status, p.EndDate, p.ProgressPercentage })
            .ToListAsync();

        var health = await ClassifyProjectsAsync(filter);
        var workloadBars = await GetWorkloadBarsAsync(filter);

        var completedTasks = await tasks
            .Where(t => t.Status == TaskStatus.Completed)
            .Select(t => new { t.DueDate, t.UpdatedAt, ReviewedAt = t.CompletionReport != null ? t.CompletionReport.ReviewedAt : null })
            .ToListAsync();

        var onTime = completedTasks.Count(t =>
        {
            var completedAt = t.ReviewedAt ?? t.UpdatedAt;
            return completedAt.Date <= t.DueDate.Date;
        });

        var totalEngineers = await _context.Engineers.AsNoTracking().CountAsync();
        var activeEngineers = await _context.Engineers.AsNoTracking().CountAsync(e => e.User.IsActive);

        if (filter.EngineerId.HasValue)
        {
            totalEngineers = await _context.Engineers.AsNoTracking().CountAsync(e => e.Id == filter.EngineerId.Value);
            activeEngineers = await _context.Engineers.AsNoTracking()
                .CountAsync(e => e.Id == filter.EngineerId.Value && e.User.IsActive);
        }
        else
        {
            var projectIds = projectList.Select(p => p.Id).ToList();
            if (projectIds.Count > 0)
            {
                var engineerIds = await _context.ProjectAssignments.AsNoTracking()
                    .Where(pa => projectIds.Contains(pa.ProjectId))
                    .Select(pa => pa.EngineerId)
                    .Distinct()
                    .ToListAsync();
                totalEngineers = engineerIds.Count;
                activeEngineers = await _context.Engineers.AsNoTracking()
                    .CountAsync(e => engineerIds.Contains(e.Id) && e.User.IsActive);
            }
        }

        return new ExecutiveSummaryDto
        {
            TotalProjects = projectList.Count,
            HealthyProjects = health.Healthy,
            AtRiskProjects = health.AtRisk + health.Critical,
            DelayedProjects = projectList.Count(p => p.EndDate.Date < today && p.Status != ProjectStatus.Completed),
            TotalEngineers = totalEngineers,
            ActiveEngineers = activeEngineers,
            TotalTasks = await tasks.CountAsync(),
            CompletedTasks = completedTasks.Count,
            OverdueTasks = await tasks.CountAsync(t => t.DueDate.Date < today && t.Status != TaskStatus.Completed),
            AverageProjectCompletion = Math.Round(projectList.Count == 0 ? 0 : projectList.Average(p => p.ProgressPercentage), 1),
            OnTimeCompletionRate = Math.Round(completedTasks.Count == 0 ? 0 : onTime * 100.0 / completedTasks.Count, 1),
            AverageEngineerWorkload = Math.Round(workloadBars.Any() ? workloadBars.Average(w => w.WorkloadPercent) : 0, 1)
        };
    }

    public async Task<ProjectHealthDto> GetProjectHealthAsync(ReportFilterQuery filter)
        => await ClassifyProjectsAsync(filter);

    public async Task<IEnumerable<ProjectProgressPointDto>> GetProjectProgressTimelineAsync(ReportFilterQuery filter)
    {
        var projects = ApplyProjectFilters(_context.Projects.AsNoTracking(), filter);
        var tasks = ApplyTaskFilters(_context.Tasks.AsNoTracking(), filter, projects);
        var taskIds = await tasks.Select(t => t.Id).ToListAsync();

        var (rangeStart, rangeEnd) = GetMonthRange(filter);
        var months = EnumerateMonths(rangeStart, rangeEnd).ToList();

        if (taskIds.Count == 0)
        {
            return months.Select(m => new ProjectProgressPointDto
            {
                Year = m.Year,
                Month = m.Month,
                Label = m.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                AverageCompletionPercent = 0
            });
        }

        var logs = await _context.TaskProgressLogs.AsNoTracking()
            .Where(l => taskIds.Contains(l.TaskId) && l.CreatedAt <= rangeEnd)
            .Select(l => new { l.TaskId, l.NewProgress, l.CreatedAt })
            .ToListAsync();

        var taskBaselines = await tasks
            .Select(t => new { t.Id, t.CompletionPercentage, t.StartDate })
            .ToListAsync();

        var result = new List<ProjectProgressPointDto>();
        foreach (var monthStart in months)
        {
            var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
            var progressValues = new List<double>();

            foreach (var task in taskBaselines)
            {
                if (task.StartDate > monthEnd)
                    continue;

                var latest = logs
                    .Where(l => l.TaskId == task.Id && l.CreatedAt <= monthEnd)
                    .OrderByDescending(l => l.CreatedAt)
                    .FirstOrDefault();

                progressValues.Add(latest?.NewProgress ?? (monthEnd >= DateTime.UtcNow ? task.CompletionPercentage : 0));
            }

            result.Add(new ProjectProgressPointDto
            {
                Year = monthStart.Year,
                Month = monthStart.Month,
                Label = monthStart.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                AverageCompletionPercent = Math.Round(progressValues.Count == 0 ? 0 : progressValues.Average(), 1)
            });
        }

        return result;
    }

    public async Task<IEnumerable<EngineerPerformanceReportRowDto>> GetEngineerPerformanceReportAsync(
        ReportFilterQuery filter)
    {
        var projectIds = await ApplyProjectFilters(_context.Projects.AsNoTracking(), filter)
            .Select(p => p.Id)
            .ToListAsync();

        var engineerQuery = _context.Engineers.AsNoTracking().AsQueryable();
        if (filter.EngineerId.HasValue)
            engineerQuery = engineerQuery.Where(e => e.Id == filter.EngineerId.Value);
        else if (projectIds.Count > 0)
            engineerQuery = engineerQuery.Where(e => e.ProjectAssignments.Any(pa => projectIds.Contains(pa.ProjectId)));

        var today = DateTime.UtcNow.Date;

        var rows = await engineerQuery
            .Select(e => new EngineerPerformanceReportRowDto
            {
                EngineerId = e.Id,
                EngineerName = e.User.FullName,
                Projects = e.ProjectAssignments.Count(pa => projectIds.Count == 0 || projectIds.Contains(pa.ProjectId)),
                CompletedTasks = e.AssignedTasks.Count(t =>
                    (projectIds.Count == 0 || projectIds.Contains(t.ProjectId)) &&
                    t.Status == TaskStatus.Completed),
                AverageCompletionPercent = e.AssignedTasks
                    .Where(t => projectIds.Count == 0 || projectIds.Contains(t.ProjectId))
                    .Select(t => (double?)t.CompletionPercentage)
                    .Average() ?? 0,
                OverdueTasks = e.AssignedTasks.Count(t =>
                    (projectIds.Count == 0 || projectIds.Contains(t.ProjectId)) &&
                    t.DueDate.Date < today &&
                    t.Status != TaskStatus.Completed),
                CurrentWorkloadPercent = 0,
                AverageDelayDays = 0,
                OnTimeRate = 0,
                PerformanceScore = 0
            })
            .ToListAsync();

        var engineerIds = rows.Select(r => r.EngineerId).ToList();
        var completed = await _context.Tasks.AsNoTracking()
            .Where(t =>
                t.AssignedEngineerId != null &&
                engineerIds.Contains(t.AssignedEngineerId.Value) &&
                t.Status == TaskStatus.Completed &&
                (projectIds.Count == 0 || projectIds.Contains(t.ProjectId)))
            .Select(t => new
            {
                EngineerId = t.AssignedEngineerId!.Value,
                t.DueDate,
                CompletedAt = t.CompletionReport != null && t.CompletionReport.ReviewedAt.HasValue
                    ? t.CompletionReport.ReviewedAt.Value
                    : t.UpdatedAt
            })
            .ToListAsync();

        var activeCounts = await _context.Tasks.AsNoTracking()
            .Where(t =>
                t.AssignedEngineerId != null &&
                engineerIds.Contains(t.AssignedEngineerId.Value) &&
                t.Status != TaskStatus.Completed &&
                (projectIds.Count == 0 || projectIds.Contains(t.ProjectId)))
            .GroupBy(t => t.AssignedEngineerId!.Value)
            .Select(g => new { EngineerId = g.Key, Active = g.Count() })
            .ToListAsync();

        var activeMap = activeCounts.ToDictionary(x => x.EngineerId, x => x.Active);

        foreach (var row in rows)
        {
            var engCompleted = completed.Where(c => c.EngineerId == row.EngineerId).ToList();
            var onTime = engCompleted.Count(c => c.CompletedAt.Date <= c.DueDate.Date);
            row.OnTimeRate = Math.Round(engCompleted.Count == 0 ? 0 : onTime * 100.0 / engCompleted.Count, 1);
            row.AverageDelayDays = Math.Round(
                engCompleted.Count == 0
                    ? 0
                    : engCompleted.Average(c => (c.CompletedAt.Date - c.DueDate.Date).TotalDays),
                1);
            row.AverageCompletionPercent = Math.Round(row.AverageCompletionPercent, 1);

            var active = activeMap.GetValueOrDefault(row.EngineerId, 0);
            row.CurrentWorkloadPercent = ClampWorkloadPercent(active);
            row.CurrentWorkloadLevel = active switch
            {
                <= 5 => WorkloadLevel.Low,
                <= 10 => WorkloadLevel.Medium,
                _ => WorkloadLevel.High
            };

            var delayScore = engCompleted.Count == 0
                ? 50
                : row.AverageDelayDays <= 0
                    ? 100
                    : Math.Max(0, 100 - row.AverageDelayDays * 10);
            row.PerformanceScore = Math.Round(
                row.OnTimeRate * 0.4 +
                Math.Min(100, row.AverageCompletionPercent) * 0.3 +
                delayScore * 0.3,
                1);
        }

        var ranked = rows.OrderByDescending(r => r.PerformanceScore).ThenBy(r => r.EngineerName).ToList();
        if (ranked.Count > 0)
            ranked[0].IsTopPerformer = true;

        return ranked;
    }

    public async Task<IEnumerable<WorkloadBarDto>> GetWorkloadBarsAsync(ReportFilterQuery filter)
    {
        var projectIds = await ApplyProjectFilters(_context.Projects.AsNoTracking(), filter)
            .Select(p => p.Id)
            .ToListAsync();

        var engineerQuery = _context.Engineers.AsNoTracking().AsQueryable();
        if (filter.EngineerId.HasValue)
            engineerQuery = engineerQuery.Where(e => e.Id == filter.EngineerId.Value);
        else if (projectIds.Count > 0)
            engineerQuery = engineerQuery.Where(e => e.ProjectAssignments.Any(pa => projectIds.Contains(pa.ProjectId)));

        var today = DateTime.UtcNow.Date;

        var bars = await engineerQuery
            .Select(e => new WorkloadBarDto
            {
                EngineerId = e.Id,
                EngineerName = e.User.FullName,
                ActiveTasks = e.AssignedTasks.Count(t =>
                    (projectIds.Count == 0 || projectIds.Contains(t.ProjectId)) &&
                    t.Status != TaskStatus.Completed),
                OverdueTasks = e.AssignedTasks.Count(t =>
                    (projectIds.Count == 0 || projectIds.Contains(t.ProjectId)) &&
                    t.DueDate.Date < today &&
                    t.Status != TaskStatus.Completed)
            })
            .ToListAsync();

        foreach (var bar in bars)
            bar.WorkloadPercent = ClampWorkloadPercent(bar.ActiveTasks);

        return bars.OrderByDescending(b => b.WorkloadPercent).ThenByDescending(b => b.ActiveTasks).ToList();
    }

    public async Task<TaskAnalyticsDto> GetTaskAnalyticsAsync(ReportFilterQuery filter)
    {
        var projects = ApplyProjectFilters(_context.Projects.AsNoTracking(), filter);
        var tasks = ApplyTaskFilters(_context.Tasks.AsNoTracking(), filter, projects);
        var today = DateTime.UtcNow.Date;

        var priorityGrouped = await tasks
            .GroupBy(t => t.Priority)
            .Select(g => new { Priority = g.Key, Count = g.Count() })
            .ToListAsync();

        var statusGrouped = await tasks
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var overdue = await tasks.CountAsync(t => t.DueDate.Date < today && t.Status != TaskStatus.Completed);
        var completed = statusGrouped.FirstOrDefault(x => x.Status == TaskStatus.Completed)?.Count ?? 0;

        var (rangeStart, rangeEnd) = GetMonthRange(filter);
        var completions = await tasks
            .Where(t => t.Status == TaskStatus.Completed &&
                        t.UpdatedAt >= rangeStart &&
                        t.UpdatedAt <= rangeEnd)
            .Select(t => t.UpdatedAt)
            .ToListAsync();

        var trend = EnumerateMonths(rangeStart, rangeEnd)
            .Select(m =>
            {
                var count = completions.Count(d => d.Year == m.Year && d.Month == m.Month);
                return new MonthlyCountPointDto
                {
                    Year = m.Year,
                    Month = m.Month,
                    Label = m.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                    Count = count
                };
            })
            .ToList();

        return new TaskAnalyticsDto
        {
            ByPriority = new TaskPriorityBreakdownDto
            {
                Low = priorityGrouped.FirstOrDefault(x => x.Priority == TaskPriority.Low)?.Count ?? 0,
                Medium = priorityGrouped.FirstOrDefault(x => x.Priority == TaskPriority.Medium)?.Count ?? 0,
                High = priorityGrouped.FirstOrDefault(x => x.Priority == TaskPriority.High)?.Count ?? 0,
                Critical = priorityGrouped.FirstOrDefault(x => x.Priority == TaskPriority.Critical)?.Count ?? 0
            },
            ByStatus = new TaskStatusBreakdownDto
            {
                NotStarted = statusGrouped.FirstOrDefault(x => x.Status == TaskStatus.NotStarted)?.Count ?? 0,
                InProgress = statusGrouped.FirstOrDefault(x => x.Status == TaskStatus.InProgress)?.Count ?? 0,
                PendingReview = statusGrouped.FirstOrDefault(x => x.Status == TaskStatus.PendingReview)?.Count ?? 0,
                Completed = completed,
                Blocked = statusGrouped.FirstOrDefault(x => x.Status == TaskStatus.Blocked)?.Count ?? 0,
                Ready = statusGrouped.FirstOrDefault(x => x.Status == TaskStatus.Ready)?.Count ?? 0
            },
            OverdueVsCompleted = new OverdueVsCompletedDto
            {
                Overdue = overdue,
                Completed = completed
            },
            CompletionTrend = trend
        };
    }

    public async Task<IEnumerable<ReportActivityDto>> GetActivityAsync(ReportFilterQuery filter)
    {
        var projectIds = await ApplyProjectFilters(_context.Projects.AsNoTracking(), filter)
            .Select(p => p.Id)
            .ToListAsync();

        var activities = new List<ReportActivityDto>();

        var assignments = await _context.ProjectAssignments.AsNoTracking()
            .Where(pa => projectIds.Count == 0 || projectIds.Contains(pa.ProjectId))
            .OrderByDescending(pa => pa.AssignedDate)
            .Take(30)
            .Select(pa => new ReportActivityDto
            {
                Time = pa.AssignedDate,
                User = pa.Engineer.User.FullName,
                Action = "Engineer assigned",
                ProjectName = pa.Project.Name,
                ProjectId = pa.ProjectId
            })
            .ToListAsync();
        activities.AddRange(assignments);

        var taskCreated = await _context.Tasks.AsNoTracking()
            .Where(t => projectIds.Count == 0 || projectIds.Contains(t.ProjectId))
            .OrderByDescending(t => t.StartDate)
            .Take(30)
            .Select(t => new ReportActivityDto
            {
                Time = t.StartDate,
                User = t.AssignedEngineer != null ? t.AssignedEngineer.User.FullName : "System",
                Action = "Task created",
                ProjectName = t.Project.Name,
                TaskTitle = t.Title,
                ProjectId = t.ProjectId,
                TaskId = t.Id
            })
            .ToListAsync();
        activities.AddRange(taskCreated);

        var taskCompleted = await _context.Tasks.AsNoTracking()
            .Where(t => t.Status == TaskStatus.Completed &&
                        (projectIds.Count == 0 || projectIds.Contains(t.ProjectId)))
            .OrderByDescending(t => t.UpdatedAt)
            .Take(30)
            .Select(t => new ReportActivityDto
            {
                Time = t.UpdatedAt,
                User = t.AssignedEngineer != null ? t.AssignedEngineer.User.FullName : "System",
                Action = "Task completed",
                ProjectName = t.Project.Name,
                TaskTitle = t.Title,
                ProjectId = t.ProjectId,
                TaskId = t.Id
            })
            .ToListAsync();
        activities.AddRange(taskCompleted);

        var progress = await _context.TaskProgressLogs.AsNoTracking()
            .Where(l => projectIds.Count == 0 || projectIds.Contains(l.Task.ProjectId))
            .OrderByDescending(l => l.CreatedAt)
            .Take(30)
            .Select(l => new ReportActivityDto
            {
                Time = l.CreatedAt,
                User = l.Engineer.User.FullName,
                Action = "Progress updated",
                ProjectName = l.Task.Project.Name,
                TaskTitle = l.Task.Title,
                ProjectId = l.Task.ProjectId,
                TaskId = l.TaskId
            })
            .ToListAsync();
        activities.AddRange(progress);

        var documents = await _context.Documents.AsNoTracking()
            .Where(d => projectIds.Count == 0 || projectIds.Contains(d.ProjectId))
            .OrderByDescending(d => d.UploadDate)
            .Take(30)
            .Select(d => new ReportActivityDto
            {
                Time = d.UploadDate,
                User = d.UploadedByUser.FullName,
                Action = "Document uploaded",
                ProjectName = d.Project.Name,
                ProjectId = d.ProjectId
            })
            .ToListAsync();
        activities.AddRange(documents);

        var reports = await _context.TaskCompletionReports.AsNoTracking()
            .Where(r => projectIds.Count == 0 || projectIds.Contains(r.Task.ProjectId))
            .OrderByDescending(r => r.UploadedAt)
            .Take(30)
            .Select(r => new ReportActivityDto
            {
                Time = r.UploadedAt,
                User = r.UploadedByUser.FullName,
                Action = "Completion report submitted",
                ProjectName = r.Task.Project.Name,
                TaskTitle = r.Task.Title,
                ProjectId = r.Task.ProjectId,
                TaskId = r.TaskId
            })
            .ToListAsync();
        activities.AddRange(reports);

        return activities
            .OrderByDescending(a => a.Time)
            .Take(40)
            .ToList();
    }

    public async Task<IEnumerable<AttentionProjectDto>> GetAttentionProjectsAsync(ReportFilterQuery filter)
    {
        var risks = await LoadFilteredProjectRisksAsync(filter);
        var today = DateTime.UtcNow.Date;
        var staleCutoff = today.AddDays(-StaleProgressDays);

        var projectIds = risks.Select(r => r.Id).ToList();
        var engineers = await _context.ProjectAssignments.AsNoTracking()
            .Where(pa => projectIds.Contains(pa.ProjectId))
            .Select(pa => new { pa.ProjectId, Name = pa.Engineer.User.FullName })
            .ToListAsync();

        var recentLogs = await _context.TaskProgressLogs.AsNoTracking()
            .Where(l => projectIds.Contains(l.Task.ProjectId) && l.CreatedAt >= staleCutoff)
            .Select(l => l.Task.ProjectId)
            .Distinct()
            .ToListAsync();
        var projectsWithRecentProgress = recentLogs.ToHashSet();

        var attentionByProject = new Dictionary<int, AttentionProjectDto>();

        foreach (var risk in risks)
        {
            if (risk.Status == ProjectStatus.Completed)
                continue;

            var reasons = new List<string>();
            if (risk.OverdueTaskCount > 0)
                reasons.Add($"{risk.OverdueTaskCount} overdue task(s)");
            if (risk.RiskLevel >= RiskLevel.High)
                reasons.Add($"Risk: {risk.RiskLevel}");

            var duration = Math.Max(1, (risk.EndDate.Date - risk.StartDate.Date).Days);
            var elapsed = Math.Clamp((today - risk.StartDate.Date).Days, 0, duration);
            var expected = Math.Round(elapsed / (double)duration * 100, 0);
            if (risk.ProgressPercentage + 10 < expected)
                reasons.Add("Completion behind schedule");

            if (!projectsWithRecentProgress.Contains(risk.Id) && risk.ActiveTaskCount > 0)
                reasons.Add("No recent progress updates");

            if (reasons.Count == 0)
                continue;

            attentionByProject[risk.Id] = new AttentionProjectDto
            {
                ProjectId = risk.Id,
                ProjectName = risk.Name,
                RiskLevel = risk.RiskLevel,
                CompletionPercent = risk.ProgressPercentage,
                OverdueTasks = risk.OverdueTaskCount,
                AssignedEngineers = engineers
                    .Where(e => e.ProjectId == risk.Id)
                    .Select(e => e.Name)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList(),
                Reason = string.Join(" · ", reasons)
            };
        }

        // Forecast-driven attention: DelayedStart, AtRisk, and Delayed always appear.
        var forecastProjects = await ApplyProjectFilters(
                _context.Projects.AsNoTracking().Include(p => p.Tasks),
                filter)
            .Where(p => p.Status != ProjectStatus.Completed)
            .ToListAsync();

        var forecastProjectIds = forecastProjects.Select(p => p.Id).ToList();
        var forecastEngineers = await _context.ProjectAssignments.AsNoTracking()
            .Where(pa => forecastProjectIds.Contains(pa.ProjectId))
            .Select(pa => new { pa.ProjectId, Name = pa.Engineer.User.FullName })
            .ToListAsync();

        foreach (var project in forecastProjects)
        {
            var prediction = _predictionService.CalculatePrediction(project);
            if (prediction.PredictionState is not (
                PredictionState.DelayedStart
                or PredictionState.AtRisk
                or PredictionState.Delayed))
            {
                continue;
            }

            var forecastRisk = MapPredictionToRiskLevel(prediction.PredictionState);
            var engineerNames = forecastEngineers
                .Where(e => e.ProjectId == project.Id)
                .Select(e => e.Name)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            if (attentionByProject.TryGetValue(project.Id, out var existing))
            {
                existing.PredictionState = prediction.PredictionState;
                if (forecastRisk > existing.RiskLevel)
                {
                    existing.RiskLevel = forecastRisk;
                }

                if (!existing.Reason.Contains(prediction.StatusLabel, StringComparison.OrdinalIgnoreCase))
                {
                    existing.Reason = string.IsNullOrWhiteSpace(existing.Reason)
                        ? prediction.RiskMessage
                        : $"{existing.Reason} · {prediction.RiskMessage}";
                }
            }
            else
            {
                attentionByProject[project.Id] = new AttentionProjectDto
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    RiskLevel = forecastRisk,
                    CompletionPercent = (int)Math.Round(prediction.CurrentProgress, MidpointRounding.AwayFromZero),
                    OverdueTasks = 0,
                    AssignedEngineers = engineerNames,
                    Reason = prediction.RiskMessage,
                    PredictionState = prediction.PredictionState
                };
            }
        }

        return attentionByProject.Values
            .OrderByDescending(a => a.RiskLevel)
            .ThenByDescending(a => a.OverdueTasks)
            .ThenBy(a => a.ProjectName)
            .ToList();
    }

    private static RiskLevel MapPredictionToRiskLevel(PredictionState state) => state switch
    {
        PredictionState.Delayed => RiskLevel.Critical,
        PredictionState.AtRisk => RiskLevel.High,
        PredictionState.DelayedStart => RiskLevel.Medium,
        _ => RiskLevel.Low
    };

    #region Legacy

    public async Task<ReportsSummaryDto> GetSummaryAsync(ReportFilterQuery filter)
    {
        var executive = await GetExecutiveSummaryAsync(filter);
        var projects = ApplyProjectFilters(_context.Projects.AsNoTracking(), filter);
        return new ReportsSummaryDto
        {
            TotalProjects = executive.TotalProjects,
            ActiveProjects = await projects.CountAsync(p => p.Status == ProjectStatus.InProgress),
            CompletedProjects = await projects.CountAsync(p => p.Status == ProjectStatus.Completed),
            DelayedProjects = executive.DelayedProjects,
            TotalEngineers = executive.TotalEngineers,
            TotalTasks = executive.TotalTasks,
            CompletedTasks = executive.CompletedTasks,
            OverdueTasks = executive.OverdueTasks,
            AverageProjectProgress = executive.AverageProjectCompletion,
            TotalDocuments = await GetDocumentsQuery(filter, projects).CountAsync()
        };
    }

    public async Task<ProjectStatusDistributionDto> GetProjectStatusDistributionAsync(ReportFilterQuery filter)
    {
        var grouped = await ApplyProjectFilters(_context.Projects.AsNoTracking(), filter)
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return new ProjectStatusDistributionDto
        {
            Completed = grouped.FirstOrDefault(x => x.Status == ProjectStatus.Completed)?.Count ?? 0,
            InProgress = grouped.FirstOrDefault(x => x.Status == ProjectStatus.InProgress)?.Count ?? 0,
            NotStarted = grouped.FirstOrDefault(x => x.Status == ProjectStatus.NotStarted)?.Count ?? 0
        };
    }

    public async Task<TasksByPriorityDto> GetTasksByPriorityAsync(ReportFilterQuery filter)
    {
        var analytics = await GetTaskAnalyticsAsync(filter);
        return new TasksByPriorityDto
        {
            Low = analytics.ByPriority.Low,
            Medium = analytics.ByPriority.Medium,
            High = analytics.ByPriority.High,
            Critical = analytics.ByPriority.Critical
        };
    }

    public async Task<TasksByStatusDto> GetTasksByStatusAsync(ReportFilterQuery filter)
    {
        var analytics = await GetTaskAnalyticsAsync(filter);
        return new TasksByStatusDto
        {
            NotStarted = analytics.ByStatus.NotStarted,
            InProgress = analytics.ByStatus.InProgress,
            Completed = analytics.ByStatus.Completed
        };
    }

    public async Task<IEnumerable<ProjectProgressChartDto>> GetProjectProgressAsync(ReportFilterQuery filter)
    {
        return await ApplyProjectFilters(_context.Projects.AsNoTracking(), filter)
            .OrderBy(p => p.Name)
            .Select(p => new ProjectProgressChartDto
            {
                ProjectName = p.Name,
                ProgressPercentage = p.ProgressPercentage,
                Status = p.Status
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<MonthlyProjectsDto>> GetMonthlyProjectsAsync(ReportFilterQuery filter)
    {
        var projects = ApplyProjectFilters(_context.Projects.AsNoTracking(), filter);
        var year = filter.StartDate?.Year ?? DateTime.UtcNow.Year;

        var monthlyCounts = await projects
            .Where(p => p.CreatedAt.Year == year)
            .GroupBy(p => p.CreatedAt.Month)
            .Select(g => new { Month = g.Key, Count = g.Count() })
            .ToListAsync();

        var countByMonth = monthlyCounts.ToDictionary(x => x.Month, x => x.Count);

        return Enumerable.Range(1, 12)
            .Select(month => new MonthlyProjectsDto
            {
                Month = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month),
                ProjectsCreated = countByMonth.GetValueOrDefault(month, 0)
            })
            .ToList();
    }

    public async Task<IEnumerable<EngineerWorkloadDto>> GetEngineerWorkloadAsync(ReportFilterQuery filter)
    {
        var bars = await GetWorkloadBarsAsync(filter);
        return bars.Select(b => new EngineerWorkloadDto
        {
            EngineerId = b.EngineerId,
            EngineerName = b.EngineerName,
            ActiveTasks = b.ActiveTasks,
            OverdueTasks = b.OverdueTasks,
            WorkloadLevel = b.ActiveTasks switch
            {
                <= 5 => WorkloadLevel.Low,
                <= 10 => WorkloadLevel.Medium,
                _ => WorkloadLevel.High
            }
        });
    }

    public async Task<PagedResult<ReportProjectRowDto>> GetProjectsTableAsync(
        ReportFilterQuery filter,
        string? search,
        string? sortBy,
        bool descending,
        int pageNumber,
        int pageSize)
    {
        pageNumber = pageNumber < 1 ? DefaultPageNumber : pageNumber;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var rows = await BuildProjectRowsAsync(filter, search);
        rows = ApplyRowSorting(rows, sortBy, descending);

        var totalCount = rows.Count;
        var items = rows
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<ReportProjectRowDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    #endregion

    public async Task<FileStreamResult> ExportExcelAsync(ReportFilterQuery filter, string? search)
    {
        var summary = await GetExecutiveSummaryAsync(filter);
        var health = await GetProjectHealthAsync(filter);
        var engineers = (await GetEngineerPerformanceReportAsync(filter)).ToList();
        var attention = (await GetAttentionProjectsAsync(filter)).ToList();
        var tasks = await GetTaskAnalyticsAsync(filter);

        using var workbook = new XLWorkbook();

        var summarySheet = workbook.Worksheets.Add("Executive Summary");
        summarySheet.Cell(1, 1).Value = $"{CompanyName} — Executive Report";
        summarySheet.Cell(2, 1).Value = $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";
        var summaryRows = new (string Label, object Value)[]
        {
            ("Total Projects", summary.TotalProjects),
            ("Healthy Projects", summary.HealthyProjects),
            ("At Risk Projects", summary.AtRiskProjects),
            ("Delayed Projects", summary.DelayedProjects),
            ("Total Engineers", summary.TotalEngineers),
            ("Active Engineers", summary.ActiveEngineers),
            ("Total Tasks", summary.TotalTasks),
            ("Completed Tasks", summary.CompletedTasks),
            ("Overdue Tasks", summary.OverdueTasks),
            ("Average Project Completion %", summary.AverageProjectCompletion),
            ("On-Time Completion Rate %", summary.OnTimeCompletionRate),
            ("Average Engineer Workload %", summary.AverageEngineerWorkload)
        };
        for (var i = 0; i < summaryRows.Length; i++)
        {
            summarySheet.Cell(i + 4, 1).Value = summaryRows[i].Label;
            summarySheet.Cell(i + 4, 2).Value = Convert.ToString(summaryRows[i].Value, CultureInfo.InvariantCulture);
        }

        var healthSheet = workbook.Worksheets.Add("Project Health");
        healthSheet.Cell(1, 1).Value = "Category";
        healthSheet.Cell(1, 2).Value = "Count";
        healthSheet.Cell(2, 1).Value = "Healthy";
        healthSheet.Cell(2, 2).Value = health.Healthy;
        healthSheet.Cell(3, 1).Value = "At Risk";
        healthSheet.Cell(3, 2).Value = health.AtRisk;
        healthSheet.Cell(4, 1).Value = "Critical";
        healthSheet.Cell(4, 2).Value = health.Critical;
        healthSheet.Cell(5, 1).Value = "Completed";
        healthSheet.Cell(5, 2).Value = health.Completed;

        var engSheet = workbook.Worksheets.Add("Engineer Performance");
        var engHeaders = new[]
        {
            "Engineer", "Projects", "Completed Tasks", "Avg Completion %", "On-Time Rate %",
            "Overdue", "Workload %", "Avg Delay", "Score", "Top Performer"
        };
        for (var i = 0; i < engHeaders.Length; i++)
            engSheet.Cell(1, i + 1).Value = engHeaders[i];
        for (var r = 0; r < engineers.Count; r++)
        {
            var row = engineers[r];
            var excelRow = r + 2;
            engSheet.Cell(excelRow, 1).Value = row.EngineerName;
            engSheet.Cell(excelRow, 2).Value = row.Projects;
            engSheet.Cell(excelRow, 3).Value = row.CompletedTasks;
            engSheet.Cell(excelRow, 4).Value = row.AverageCompletionPercent;
            engSheet.Cell(excelRow, 5).Value = row.OnTimeRate;
            engSheet.Cell(excelRow, 6).Value = row.OverdueTasks;
            engSheet.Cell(excelRow, 7).Value = row.CurrentWorkloadPercent;
            engSheet.Cell(excelRow, 8).Value = row.AverageDelayDays;
            engSheet.Cell(excelRow, 9).Value = row.PerformanceScore;
            engSheet.Cell(excelRow, 10).Value = row.IsTopPerformer ? "Yes" : "";
        }

        var attentionSheet = workbook.Worksheets.Add("Attention");
        var attHeaders = new[] { "Project", "Risk", "Completion %", "Overdue", "Engineers", "Reason" };
        for (var i = 0; i < attHeaders.Length; i++)
            attentionSheet.Cell(1, i + 1).Value = attHeaders[i];
        for (var r = 0; r < attention.Count; r++)
        {
            var row = attention[r];
            var excelRow = r + 2;
            attentionSheet.Cell(excelRow, 1).Value = row.ProjectName;
            attentionSheet.Cell(excelRow, 2).Value = row.RiskLevel.ToString();
            attentionSheet.Cell(excelRow, 3).Value = row.CompletionPercent;
            attentionSheet.Cell(excelRow, 4).Value = row.OverdueTasks;
            attentionSheet.Cell(excelRow, 5).Value = string.Join(", ", row.AssignedEngineers);
            attentionSheet.Cell(excelRow, 6).Value = row.Reason;
        }

        var taskSheet = workbook.Worksheets.Add("Task Analytics");
        taskSheet.Cell(1, 1).Value = "Priority Low";
        taskSheet.Cell(1, 2).Value = tasks.ByPriority.Low;
        taskSheet.Cell(2, 1).Value = "Priority Medium";
        taskSheet.Cell(2, 2).Value = tasks.ByPriority.Medium;
        taskSheet.Cell(3, 1).Value = "Priority High";
        taskSheet.Cell(3, 2).Value = tasks.ByPriority.High;
        taskSheet.Cell(4, 1).Value = "Priority Critical";
        taskSheet.Cell(4, 2).Value = tasks.ByPriority.Critical;
        taskSheet.Cell(6, 1).Value = "Overdue";
        taskSheet.Cell(6, 2).Value = tasks.OverdueVsCompleted.Overdue;
        taskSheet.Cell(7, 1).Value = "Completed";
        taskSheet.Cell(7, 2).Value = tasks.OverdueVsCompleted.Completed;

        foreach (var sheet in workbook.Worksheets)
            sheet.Columns().AdjustToContents();

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return new FileStreamResult(stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        {
            FileDownloadName = $"executive-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx"
        };
    }

    public async Task<FileStreamResult> ExportPdfAsync(ReportFilterQuery filter, string? search)
    {
        var summary = await GetExecutiveSummaryAsync(filter);
        var health = await GetProjectHealthAsync(filter);
        var engineers = (await GetEngineerPerformanceReportAsync(filter)).Take(15).ToList();
        var attention = (await GetAttentionProjectsAsync(filter)).Take(20).ToList();

        var stream = new MemoryStream();
        QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Column(col =>
                {
                    col.Item().Text(CompanyName).FontSize(11).FontColor(Colors.Grey.Darken2);
                    col.Item().Text("Executive Analytics Report").FontSize(18).SemiBold();
                });
                page.Content().Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").FontSize(10);

                    column.Item().Text("Executive Summary").FontSize(14).SemiBold();
                    column.Item().Text(
                        $"Projects: {summary.TotalProjects} | Healthy: {summary.HealthyProjects} | " +
                        $"At Risk: {summary.AtRiskProjects} | Delayed: {summary.DelayedProjects}");
                    column.Item().Text(
                        $"Engineers: {summary.TotalEngineers} active {summary.ActiveEngineers} | " +
                        $"Tasks: {summary.TotalTasks} completed {summary.CompletedTasks} overdue {summary.OverdueTasks}");
                    column.Item().Text(
                        $"Avg Completion: {summary.AverageProjectCompletion}% | " +
                        $"On-Time: {summary.OnTimeCompletionRate}% | " +
                        $"Avg Workload: {summary.AverageEngineerWorkload}%");

                    column.Item().PaddingTop(6).Text("Project Health").FontSize(14).SemiBold();
                    column.Item().Text(
                        $"Healthy {health.Healthy} · At Risk {health.AtRisk} · Critical {health.Critical} · Completed {health.Completed}");

                    column.Item().PaddingTop(6).Text("Engineer Performance").FontSize(14).SemiBold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });
                        table.Header(h =>
                        {
                            h.Cell().Text("Engineer").SemiBold();
                            h.Cell().Text("Done").SemiBold();
                            h.Cell().Text("On-Time").SemiBold();
                            h.Cell().Text("Workload").SemiBold();
                            h.Cell().Text("Score").SemiBold();
                        });
                        foreach (var row in engineers)
                        {
                            table.Cell().Text(row.IsTopPerformer ? $"★ {row.EngineerName}" : row.EngineerName);
                            table.Cell().Text(row.CompletedTasks.ToString());
                            table.Cell().Text($"{row.OnTimeRate}%");
                            table.Cell().Text($"{row.CurrentWorkloadPercent}%");
                            table.Cell().Text(row.PerformanceScore.ToString("0.0"));
                        }
                    });

                    column.Item().PaddingTop(6).Text("Projects Requiring Attention").FontSize(14).SemiBold();
                    if (attention.Count == 0)
                    {
                        column.Item().Text("No projects currently require attention.").FontSize(10);
                    }
                    else
                    {
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn(3);
                            });
                            table.Header(h =>
                            {
                                h.Cell().Text("Project").SemiBold();
                                h.Cell().Text("Risk").SemiBold();
                                h.Cell().Text("Overdue").SemiBold();
                                h.Cell().Text("Reason").SemiBold();
                            });
                            foreach (var row in attention)
                            {
                                table.Cell().Text(row.ProjectName);
                                table.Cell().Text(row.RiskLevel.ToString());
                                table.Cell().Text(row.OverdueTasks.ToString());
                                table.Cell().Text(row.Reason).FontSize(9);
                            }
                        });
                    }
                });
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span($"{CompanyName} · Page ");
                    text.CurrentPageNumber();
                });
            });
        }).GeneratePdf(stream);

        stream.Position = 0;
        return new FileStreamResult(stream, "application/pdf")
        {
            FileDownloadName = $"executive-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf"
        };
    }

    private async Task<ProjectHealthDto> ClassifyProjectsAsync(ReportFilterQuery filter)
    {
        var risks = await LoadFilteredProjectRisksAsync(filter);
        var health = new ProjectHealthDto();

        foreach (var risk in risks)
        {
            if (risk.Status == ProjectStatus.Completed)
            {
                health.Completed++;
                continue;
            }

            if (risk.RiskLevel == RiskLevel.Critical)
                health.Critical++;
            else if (risk.RiskLevel is RiskLevel.Medium or RiskLevel.High)
                health.AtRisk++;
            else
                health.Healthy++;
        }

        return health;
    }

    private async Task<List<ProjectRiskDto>> LoadFilteredProjectRisksAsync(ReportFilterQuery filter)
    {
        var filteredIds = await ApplyProjectFilters(_context.Projects.AsNoTracking(), filter)
            .Select(p => p.Id)
            .ToListAsync();

        if (filteredIds.Count == 0)
            return [];

        var risks = new List<ProjectRiskDto>();
        var pageNumber = 1;
        const int pageSize = 100;
        while (true)
        {
            var page = await _riskAnalysisService.GetProjectRisksAsync(
                userId: 0,
                isAdmin: true,
                search: null,
                riskLevel: null,
                sortBy: null,
                descending: false,
                pageNumber: pageNumber,
                pageSize: pageSize);

            risks.AddRange(page.Items.Where(r => filteredIds.Contains(r.Id)));
            if (page.Items.Count < pageSize || pageNumber * pageSize >= page.TotalCount)
                break;
            pageNumber++;
        }

        if (filter.RiskLevel.HasValue)
            risks = risks.Where(r => r.RiskLevel == filter.RiskLevel.Value).ToList();

        return risks;
    }

    private async Task<List<ReportProjectRowDto>> BuildProjectRowsAsync(ReportFilterQuery filter, string? search)
    {
        var today = DateTime.UtcNow.Date;
        var query = ApplyProjectFilters(_context.Projects.AsNoTracking(), filter);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term));
        }

        var projects = await query
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.ProgressPercentage,
                OpenTasks = p.Tasks.Count(t => t.Status != TaskStatus.Completed),
                CompletedTasks = p.Tasks.Count(t => t.Status == TaskStatus.Completed),
                OverdueTasks = p.Tasks.Count(t =>
                    t.DueDate.Date < today && t.Status != TaskStatus.Completed),
                DocumentsCount = p.Documents.Count,
                EngineersAssigned = p.ProjectAssignments.Count
            })
            .ToListAsync();

        return projects.Select(p => new ReportProjectRowDto
        {
            ProjectId = p.Id,
            ProjectName = p.Name,
            Manager = null,
            ProgressPercentage = p.ProgressPercentage,
            OpenTasks = p.OpenTasks,
            CompletedTasks = p.CompletedTasks,
            OverdueTasks = p.OverdueTasks,
            DocumentsCount = p.DocumentsCount,
            EngineersAssigned = p.EngineersAssigned
        }).ToList();
    }

    private static List<ReportProjectRowDto> ApplyRowSorting(
        List<ReportProjectRowDto> rows,
        string? sortBy,
        bool descending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return rows.OrderBy(r => r.ProjectName).ToList();

        Func<IEnumerable<ReportProjectRowDto>, IOrderedEnumerable<ReportProjectRowDto>> order = sortBy.Trim().ToLowerInvariant() switch
        {
            "progresspercentage" or "progress" => q => descending
                ? q.OrderByDescending(r => r.ProgressPercentage)
                : q.OrderBy(r => r.ProgressPercentage),
            "opentasks" or "open" => q => descending
                ? q.OrderByDescending(r => r.OpenTasks)
                : q.OrderBy(r => r.OpenTasks),
            "completedtasks" or "completed" => q => descending
                ? q.OrderByDescending(r => r.CompletedTasks)
                : q.OrderBy(r => r.CompletedTasks),
            "overduetasks" or "overdue" => q => descending
                ? q.OrderByDescending(r => r.OverdueTasks)
                : q.OrderBy(r => r.OverdueTasks),
            "documentscount" or "documents" => q => descending
                ? q.OrderByDescending(r => r.DocumentsCount)
                : q.OrderBy(r => r.DocumentsCount),
            "engineersassigned" or "engineers" => q => descending
                ? q.OrderByDescending(r => r.EngineersAssigned)
                : q.OrderBy(r => r.EngineersAssigned),
            _ => q => descending
                ? q.OrderByDescending(r => r.ProjectName)
                : q.OrderBy(r => r.ProjectName)
        };

        return order(rows).ToList();
    }

    private IQueryable<Entities.Document> GetDocumentsQuery(ReportFilterQuery filter, IQueryable<Project> projects)
    {
        var projectIds = projects.Select(p => p.Id);
        return _context.Documents.AsNoTracking().Where(d => projectIds.Contains(d.ProjectId));
    }

    private static IQueryable<Project> ApplyProjectFilters(IQueryable<Project> query, ReportFilterQuery filter)
    {
        if (filter.ProjectId.HasValue)
            query = query.Where(p => p.Id == filter.ProjectId.Value);

        if (filter.Status.HasValue)
            query = query.Where(p => p.Status == filter.Status.Value);

        if (filter.EngineerId.HasValue)
            query = query.Where(p => p.ProjectAssignments.Any(pa => pa.EngineerId == filter.EngineerId.Value));

        if (filter.StartDate.HasValue)
            query = query.Where(p => p.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(p => p.CreatedAt <= end);
        }

        return query;
    }

    private static IQueryable<TaskItem> ApplyTaskFilters(
        IQueryable<TaskItem> query,
        ReportFilterQuery filter,
        IQueryable<Project> filteredProjects)
    {
        query = query.Where(t => filteredProjects.Select(p => p.Id).Contains(t.ProjectId));

        if (filter.ProjectId.HasValue)
            query = query.Where(t => t.ProjectId == filter.ProjectId.Value);

        if (filter.EngineerId.HasValue)
            query = query.Where(t => t.AssignedEngineerId == filter.EngineerId.Value);

        if (filter.StartDate.HasValue)
            query = query.Where(t => t.StartDate >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(t => t.DueDate <= end);
        }

        return query;
    }

    private static double ClampWorkloadPercent(int activeTasks)
        => Math.Clamp(activeTasks / (double)SoftWorkloadCapacity * 100.0, 0, 100);

    private static (DateTime Start, DateTime End) GetMonthRange(ReportFilterQuery filter)
    {
        var end = filter.EndDate?.Date ?? DateTime.UtcNow.Date;
        var start = filter.StartDate?.Date ?? end.AddMonths(-11);
        start = new DateTime(start.Year, start.Month, 1);
        end = new DateTime(end.Year, end.Month, 1).AddMonths(1).AddTicks(-1);
        return (start, end);
    }

    private static IEnumerable<DateTime> EnumerateMonths(DateTime rangeStart, DateTime rangeEnd)
    {
        var cursor = new DateTime(rangeStart.Year, rangeStart.Month, 1);
        var last = new DateTime(rangeEnd.Year, rangeEnd.Month, 1);
        while (cursor <= last)
        {
            yield return cursor;
            cursor = cursor.AddMonths(1);
        }
    }
}
