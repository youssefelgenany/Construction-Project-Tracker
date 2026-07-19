using System.Globalization;
using ConstructionProjectTracker.API.Data;
using ConstructionProjectTracker.API.DTOs.Dashboard;
using ConstructionProjectTracker.API.DTOs.Engineers;
using ConstructionProjectTracker.API.Enums;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConstructionProjectTracker.API.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly IEngineerService _engineerService;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        ApplicationDbContext context,
        IEngineerService engineerService,
        ILogger<DashboardService> logger)
    {
        _context = context;
        _engineerService = engineerService;
        _logger = logger;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(int userId, bool isAdmin)
    {
        _logger.LogInformation(
            "Dashboard summary requested. UserId={UserId}, IsAdmin={IsAdmin}",
            userId, isAdmin);

        if (isAdmin)
            return await GetAdminSummaryAsync();

        return await GetEngineerSummaryAsync(userId);
    }

    private async Task<DashboardSummaryDto> GetAdminSummaryAsync()
    {
        var projects = _context.Projects.AsNoTracking();
        var tasks = _context.Tasks.AsNoTracking();

        var summary = new DashboardSummaryDto
        {
            TotalProjects = await projects.CountAsync(),
            ActiveProjects = await projects.CountAsync(p => p.Status == ProjectStatus.InProgress),
            CompletedProjects = await projects.CountAsync(p => p.Status == ProjectStatus.Completed),
            NotStartedProjects = await projects.CountAsync(p => p.Status == ProjectStatus.NotStarted),
            TotalEngineers = await _context.Engineers.AsNoTracking().CountAsync(),
            TotalTasks = await tasks.CountAsync(),
            CompletedTasks = await tasks.CountAsync(t => t.Status == ConstructionProjectTracker.API.Enums.TaskStatus.Completed),
            PendingTasks = await tasks.CountAsync(t => t.Status != ConstructionProjectTracker.API.Enums.TaskStatus.Completed),
            AverageProjectProgress = await projects.AverageAsync(p => (double?)p.ProgressPercentage) ?? 0,
            TotalDocuments = await _context.Documents.AsNoTracking().CountAsync()
        };

        summary.AverageProjectProgress = Math.Round(summary.AverageProjectProgress, 2);
        return summary;
    }

    private async Task<DashboardSummaryDto> GetEngineerSummaryAsync(int userId)
    {
        var engineerId = await _context.Engineers
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync();

        if (engineerId is null)
        {
            return new DashboardSummaryDto();
        }

        var assignedProjectIds = _context.ProjectAssignments
            .AsNoTracking()
            .Where(pa => pa.EngineerId == engineerId.Value)
            .Select(pa => pa.ProjectId);

        var projects = _context.Projects.AsNoTracking().Where(p => assignedProjectIds.Contains(p.Id));
        var tasks = _context.Tasks.AsNoTracking().Where(t => t.AssignedEngineerId == engineerId.Value);

        var summary = new DashboardSummaryDto
        {
            TotalProjects = await projects.CountAsync(),
            ActiveProjects = await projects.CountAsync(p => p.Status == ProjectStatus.InProgress),
            CompletedProjects = await projects.CountAsync(p => p.Status == ProjectStatus.Completed),
            NotStartedProjects = await projects.CountAsync(p => p.Status == ProjectStatus.NotStarted),
            TotalEngineers = 1,
            TotalTasks = await tasks.CountAsync(),
            CompletedTasks = await tasks.CountAsync(t => t.Status == ConstructionProjectTracker.API.Enums.TaskStatus.Completed),
            PendingTasks = await tasks.CountAsync(t => t.Status != ConstructionProjectTracker.API.Enums.TaskStatus.Completed),
            AverageProjectProgress = await projects.AverageAsync(p => (double?)p.ProgressPercentage) ?? 0,
            TotalDocuments = await _context.Documents.AsNoTracking()
                .CountAsync(d => assignedProjectIds.Contains(d.ProjectId))
        };

        summary.AverageProjectProgress = Math.Round(summary.AverageProjectProgress, 2);
        return summary;
    }

    public async Task<IEnumerable<ProjectProgressChartDto>> GetProjectProgressAsync(int userId, bool isAdmin)
    {
        _logger.LogInformation(
            "Dashboard project progress requested. UserId={UserId}, IsAdmin={IsAdmin}",
            userId, isAdmin);

        var query = _context.Projects.AsNoTracking().AsQueryable();

        if (!isAdmin)
        {
            var engineerId = await _context.Engineers
                .AsNoTracking()
                .Where(e => e.UserId == userId)
                .Select(e => (int?)e.Id)
                .FirstOrDefaultAsync();

            if (engineerId is null)
                return Array.Empty<ProjectProgressChartDto>();

            query = query.Where(p => p.ProjectAssignments.Any(pa => pa.EngineerId == engineerId.Value));
        }

        return await query
            .OrderBy(p => p.Name)
            .Select(p => new ProjectProgressChartDto
            {
                ProjectName = p.Name,
                ProgressPercentage = p.ProgressPercentage,
                Status = p.Status
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<EngineerWorkloadDto>> GetEngineerWorkloadAsync(int userId, bool isAdmin)
    {
        _logger.LogInformation(
            "Engineer workload requested. UserId={UserId}, IsAdmin={IsAdmin}",
            userId, isAdmin);

        var pageSize = isAdmin ? 5 : 1;
        var result = await _engineerService.GetWorkloadAsync(
            null,
            null,
            isAdmin ? null : userId,
            1,
            pageSize);

        return result.Items;
    }

    public async Task<IEnumerable<EngineerPerformanceDto>> GetTopPerformingEngineersAsync(int userId, bool isAdmin)
    {
        _logger.LogInformation(
            "Top-performing engineers requested. UserId={UserId}, IsAdmin={IsAdmin}",
            userId, isAdmin);

        var result = await _engineerService.GetPerformanceAsync(
            null,
            "performanceScore",
            false,
            1,
            isAdmin ? 5 : 1);

        return result.Items;
    }

    public async Task<ProjectStatusDistributionDto> GetProjectStatusDistributionAsync()
    {
        _logger.LogInformation("Dashboard project status distribution requested.");

        var grouped = await _context.Projects
            .AsNoTracking()
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

    public async Task<IEnumerable<MonthlyProjectsDto>> GetMonthlyProjectsAsync()
    {
        _logger.LogInformation("Dashboard monthly projects requested.");

        var currentYear = DateTime.UtcNow.Year;

        var monthlyCounts = await _context.Projects
            .AsNoTracking()
            .Where(p => p.CreatedAt.Year == currentYear)
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

    public async Task<IEnumerable<RecentActivityDto>> GetRecentActivitiesAsync()
    {
        _logger.LogInformation("Dashboard recent activities requested.");

        var projectActivities = await _context.Projects
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Take(15)
            .Select(p => new RecentActivityDto
            {
                ActivityType = "Project Created",
                Description = $"Project '{p.Name}' was created",
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        var assignmentActivities = await _context.ProjectAssignments
            .AsNoTracking()
            .OrderByDescending(pa => pa.AssignedDate)
            .Take(15)
            .Select(pa => new RecentActivityDto
            {
                ActivityType = "Engineer Assigned",
                Description = $"{pa.Engineer.User.FullName} assigned to {pa.Project.Name}",
                CreatedAt = pa.AssignedDate
            })
            .ToListAsync();

        var taskCreatedActivities = await _context.Tasks
            .AsNoTracking()
            .OrderByDescending(t => t.StartDate)
            .Take(15)
            .Select(t => new RecentActivityDto
            {
                ActivityType = "Task Created",
                Description = $"Task '{t.Title}' was created",
                CreatedAt = t.StartDate
            })
            .ToListAsync();

        var taskCompletedActivities = await _context.Tasks
            .AsNoTracking()
            .Where(t => t.Status == ConstructionProjectTracker.API.Enums.TaskStatus.Completed)
            .OrderByDescending(t => t.UpdatedAt)
            .Take(15)
            .Select(t => new RecentActivityDto
            {
                ActivityType = "Task Completed",
                Description = $"Task '{t.Title}' was completed",
                CreatedAt = t.UpdatedAt
            })
            .ToListAsync();

        var documentActivities = await _context.Documents
            .AsNoTracking()
            .OrderByDescending(d => d.UploadDate)
            .Take(15)
            .Select(d => new RecentActivityDto
            {
                ActivityType = "Document Uploaded",
                Description = $"Document '{d.OriginalFileName}' was uploaded",
                CreatedAt = d.UploadDate
            })
            .ToListAsync();

        return projectActivities
            .Concat(assignmentActivities)
            .Concat(taskCreatedActivities)
            .Concat(taskCompletedActivities)
            .Concat(documentActivities)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .ToList();
    }
}
