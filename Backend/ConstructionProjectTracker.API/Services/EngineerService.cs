using AutoMapper;
using ConstructionProjectTracker.API.Data;
using ConstructionProjectTracker.API.DTOs.Common;
using ConstructionProjectTracker.API.DTOs.Engineers;
using ConstructionProjectTracker.API.Entities;
using ConstructionProjectTracker.API.Enums;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.EntityFrameworkCore;
using TaskStatus = ConstructionProjectTracker.API.Enums.TaskStatus;

namespace ConstructionProjectTracker.API.Services;

public class EngineerService : IEngineerService
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<EngineerService> _logger;

    public EngineerService(
        ApplicationDbContext context,
        IMapper mapper,
        IPasswordHasher passwordHasher,
        ILogger<EngineerService> logger)
    {
        _context = context;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<PagedResult<EngineerResponseDto>> GetAllAsync(
        string? search,
        string? sortBy,
        bool descending,
        int pageNumber,
        int pageSize)
    {
        pageNumber = pageNumber < 1 ? DefaultPageNumber : pageNumber;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var query = _context.Engineers
            .AsNoTracking()
            .Include(e => e.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(e =>
                e.User.FullName.ToLower().Contains(term) ||
                e.User.Email.ToLower().Contains(term) ||
                e.Position.ToLower().Contains(term));
        }

        query = ApplySorting(query, sortBy, descending);

        var totalCount = await query.CountAsync();

        var engineers = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<EngineerResponseDto>
        {
            Items = _mapper.Map<List<EngineerResponseDto>>(engineers),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<EngineerDetailsDto?> GetByIdAsync(int id)
    {
        var engineer = await _context.Engineers
            .AsNoTracking()
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (engineer is null)
            return null;

        var details = _mapper.Map<EngineerDetailsDto>(engineer);

        details.AssignedProjectsCount = await _context.ProjectAssignments
            .CountAsync(pa => pa.EngineerId == id);
        details.AssignedTasksCount = await _context.Tasks
            .CountAsync(t => t.AssignedEngineerId == id);

        return details;
    }

    public async Task<EngineerResponseDto> CreateAsync(CreateEngineerDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException("Email is already in use.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = _passwordHasher.HashPassword(dto.Password),
                Role = UserRole.Engineer,
                IsActive = true
            };

            var engineer = new Engineer
            {
                User = user,
                PhoneNumber = dto.PhoneNumber,
                Position = dto.Position,
                HireDate = dto.HireDate
            };

            _context.Engineers.Add(engineer);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Engineer created: {EngineerId} - {Email}", engineer.Id, user.Email);

            return _mapper.Map<EngineerResponseDto>(engineer);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<EngineerResponseDto?> UpdateAsync(int id, UpdateEngineerDto dto)
    {
        var engineer = await _context.Engineers
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (engineer is null)
            return null;

        engineer.User.FullName = dto.FullName;
        engineer.User.IsActive = dto.IsActive;
        engineer.PhoneNumber = dto.PhoneNumber;
        engineer.Position = dto.Position;
        engineer.HireDate = dto.HireDate;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Engineer updated: {EngineerId} - {Email}", engineer.Id, engineer.User.Email);

        return _mapper.Map<EngineerResponseDto>(engineer);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var engineer = await _context.Engineers
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (engineer is null)
            return false;

        var hasAssignments = await _context.ProjectAssignments.AnyAsync(pa => pa.EngineerId == id);
        var hasTasks = await _context.Tasks.AnyAsync(t => t.AssignedEngineerId == id);

        if (hasAssignments || hasTasks)
        {
            throw new InvalidOperationException(
                "Cannot delete engineer because they are assigned to one or more projects or tasks.");
        }

        var engineerId = engineer.Id;
        var email = engineer.User.Email;

        _context.Users.Remove(engineer.User);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Engineer deleted: {EngineerId} - {Email}", engineerId, email);

        return true;
    }

    public async Task<PagedResult<EngineerWorkloadDto>> GetWorkloadAsync(
        string? search,
        string? workloadLevel,
        int? userId,
        int pageNumber,
        int pageSize)
    {
        pageNumber = pageNumber < 1 ? DefaultPageNumber : pageNumber;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var today = DateTime.UtcNow.Date;

        var query = _context.Engineers
            .AsNoTracking()
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(e => e.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(e =>
                e.User.FullName.ToLower().Contains(term) ||
                e.User.Email.ToLower().Contains(term) ||
                e.Position.ToLower().Contains(term));
        }

        var workloads = await query
            .Select(e => new EngineerWorkloadDto
            {
                EngineerId = e.Id,
                EngineerName = e.User.FullName,
                Email = e.User.Email,
                Position = e.Position,
                PhoneNumber = e.PhoneNumber,
                HireDate = e.HireDate,
                IsActive = e.User.IsActive,
                TotalAssignedProjects = e.ProjectAssignments.Count,
                ActiveProjects = e.ProjectAssignments.Count(pa => pa.Project.Status == ProjectStatus.InProgress),
                TotalAssignedTasks = e.AssignedTasks.Count,
                ActiveTasks = e.AssignedTasks.Count(t => t.Status != TaskStatus.Completed),
                CompletedTasks = e.AssignedTasks.Count(t => t.Status == TaskStatus.Completed),
                PendingReviewTasks = e.AssignedTasks.Count(t => t.Status == TaskStatus.PendingReview),
                OverdueTasks = e.AssignedTasks.Count(t =>
                    t.DueDate.Date < today && t.Status != TaskStatus.Completed),
                AverageProgress = e.AssignedTasks.Any()
                    ? e.AssignedTasks.Average(t => (double)t.CompletionPercentage)
                    : 0,
                EarliestUpcomingDeadline = e.AssignedTasks
                    .Where(t => t.Status != TaskStatus.Completed && t.DueDate.Date >= today)
                    .OrderBy(t => t.DueDate)
                    .Select(t => (DateTime?)t.DueDate)
                    .FirstOrDefault()
            })
            .ToListAsync();

        foreach (var workload in workloads)
        {
            workload.AverageProgress = Math.Round(workload.AverageProgress, 2);
            workload.WorkloadLevel = CalculateWorkloadLevel(workload.ActiveTasks);
        }

        if (!string.IsNullOrWhiteSpace(workloadLevel) &&
            Enum.TryParse<WorkloadLevel>(workloadLevel, true, out var levelFilter))
        {
            workloads = workloads.Where(w => w.WorkloadLevel == levelFilter).ToList();
        }

        var sorted = workloads
            .OrderBy(w => w.ActiveTasks)
            .ThenBy(w => w.OverdueTasks)
            .ThenBy(w => w.EngineerName)
            .ToList();

        var totalCount = sorted.Count;
        var pagedItems = sorted
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<EngineerWorkloadDto>
        {
            Items = pagedItems,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<PagedResult<EngineerPerformanceDto>> GetPerformanceAsync(
        string? search,
        string? sortBy,
        bool descending,
        int pageNumber,
        int pageSize)
    {
        pageNumber = pageNumber < 1 ? DefaultPageNumber : pageNumber;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var query = _context.Engineers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(e =>
                e.User.FullName.ToLower().Contains(term) ||
                e.User.Email.ToLower().Contains(term) ||
                e.Position.ToLower().Contains(term));
        }

        var items = await BuildPerformanceQuery(query).ToListAsync();
        FinalizePerformanceMetrics(items);

        var sorted = ApplyPerformanceSorting(items, sortBy, descending);
        var totalCount = sorted.Count;
        var pagedItems = sorted
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<EngineerPerformanceDto>
        {
            Items = pagedItems,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<EngineerPerformanceDetailsDto?> GetPerformanceByIdAsync(int id)
    {
        var summary = await BuildPerformanceQuery(_context.Engineers.AsNoTracking().Where(e => e.Id == id))
            .FirstOrDefaultAsync();

        if (summary is null)
            return null;

        FinalizePerformanceMetrics([summary]);

        var completedTasksQuery = _context.Tasks
            .AsNoTracking()
            .Where(t => t.AssignedEngineerId == id && t.Status == TaskStatus.Completed);

        var recentCompletedTasks = await completedTasksQuery
            .OrderByDescending(t => t.CompletionReport != null && t.CompletionReport.ReviewedAt.HasValue
                ? t.CompletionReport.ReviewedAt.Value
                : t.UpdatedAt)
            .Select(t => new EngineerCompletedTaskHistoryDto
            {
                TaskId = t.Id,
                TaskTitle = t.Title,
                ProjectId = t.ProjectId,
                ProjectName = t.Project.Name,
                CompletedAt = t.CompletionReport != null && t.CompletionReport.ReviewedAt.HasValue
                    ? t.CompletionReport.ReviewedAt.Value
                    : t.UpdatedAt,
                DueDate = t.DueDate,
                FinishedOnTime = EF.Functions.DateDiffDay(
                    t.DueDate,
                    t.CompletionReport != null && t.CompletionReport.ReviewedAt.HasValue
                        ? t.CompletionReport.ReviewedAt.Value
                        : t.UpdatedAt) <= 0,
                DaysEarlyLate = EF.Functions.DateDiffDay(
                    t.DueDate,
                    t.CompletionReport != null && t.CompletionReport.ReviewedAt.HasValue
                        ? t.CompletionReport.ReviewedAt.Value
                        : t.UpdatedAt),
                DurationDays = EF.Functions.DateDiffDay(
                    t.StartDate,
                    t.CompletionReport != null && t.CompletionReport.ReviewedAt.HasValue
                        ? t.CompletionReport.ReviewedAt.Value
                        : t.UpdatedAt),
                ProgressUpdates = t.ProgressLogs.Count()
            })
            .Take(5)
            .ToListAsync();

        foreach (var task in recentCompletedTasks)
        {
            task.DurationDays = Math.Round(task.DurationDays, 1);
        }

        var recentCompletionReports = await _context.TaskCompletionReports
            .AsNoTracking()
            .Where(r => r.Task.AssignedEngineerId == id)
            .OrderByDescending(r => r.UploadedAt)
            .Select(r => new EngineerCompletionReportHistoryDto
            {
                ReportId = r.Id,
                TaskId = r.TaskId,
                TaskTitle = r.Task.Title,
                ProjectId = r.Task.ProjectId,
                ProjectName = r.Task.Project.Name,
                OriginalFileName = r.OriginalFileName,
                UploadedAt = r.UploadedAt,
                ReviewedAt = r.ReviewedAt,
                ReviewStatus = r.ReviewedAt == null
                    ? "Pending Review"
                    : (r.RejectionComment == null ? "Approved" : "Rejected")
            })
            .Take(5)
            .ToListAsync();

        var trendStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-5);
        var trendData = await completedTasksQuery
            .Where(t => (t.CompletionReport != null && t.CompletionReport.ReviewedAt.HasValue
                    ? t.CompletionReport.ReviewedAt.Value
                    : t.UpdatedAt) >= trendStart)
            .GroupBy(t => new
            {
                Year = (t.CompletionReport != null && t.CompletionReport.ReviewedAt.HasValue
                    ? t.CompletionReport.ReviewedAt.Value
                    : t.UpdatedAt).Year,
                Month = (t.CompletionReport != null && t.CompletionReport.ReviewedAt.HasValue
                    ? t.CompletionReport.ReviewedAt.Value
                    : t.UpdatedAt).Month
            })
            .Select(g => new EngineerPerformanceTrendPointDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                CompletedTasks = g.Count(),
                OnTimeRate = g.Any()
                    ? g.Count(t => EF.Functions.DateDiffDay(
                        t.DueDate,
                        t.CompletionReport != null && t.CompletionReport.ReviewedAt.HasValue
                            ? t.CompletionReport.ReviewedAt.Value
                            : t.UpdatedAt) <= 0) * 100.0 / g.Count()
                    : 0,
                PerformanceScore = 0,
                Label = string.Empty
            })
            .ToListAsync();

        foreach (var point in trendData)
        {
            point.OnTimeRate = Math.Round(point.OnTimeRate, 2);
            point.PerformanceScore = Math.Round(point.OnTimeRate * 0.7 + Math.Min(100, point.CompletedTasks * 10) * 0.3, 2);
            point.Label = new DateTime(point.Year, point.Month, 1).ToString("MMM yyyy");
        }

        trendData = trendData
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToList();

        return new EngineerPerformanceDetailsDto
        {
            Summary = summary,
            Trend = trendData,
            RecentCompletedTasks = recentCompletedTasks,
            RecentCompletionReports = recentCompletionReports
        };
    }

    private IQueryable<EngineerPerformanceDto> BuildPerformanceQuery(IQueryable<Engineer> query)
    {
        return query.Select(e => new EngineerPerformanceDto
        {
            EngineerId = e.Id,
            EngineerName = e.User.FullName,
            Email = e.User.Email,
            Position = e.Position,
            IsActive = e.User.IsActive,
            TotalProjectsWorkedOn = e.ProjectAssignments.Count(),
            ProjectsCompleted = e.ProjectAssignments.Count(pa => pa.Project.Status == ProjectStatus.Completed),
            TotalTasksAssigned = e.AssignedTasks.Count(),
            TotalTasksCompleted = e.AssignedTasks.Count(t => t.Status == TaskStatus.Completed),
            TasksFinishedBeforeDeadline = e.AssignedTasks.Count(t =>
                t.Status == TaskStatus.Completed &&
                EF.Functions.DateDiffDay(
                    t.DueDate,
                    t.CompletionReport != null && t.CompletionReport.ReviewedAt.HasValue
                        ? t.CompletionReport.ReviewedAt.Value
                        : t.UpdatedAt) <= 0),
            TasksFinishedLate = e.AssignedTasks.Count(t =>
                t.Status == TaskStatus.Completed &&
                EF.Functions.DateDiffDay(
                    t.DueDate,
                    t.CompletionReport != null && t.CompletionReport.ReviewedAt.HasValue
                        ? t.CompletionReport.ReviewedAt.Value
                        : t.UpdatedAt) > 0),
            AverageDaysEarlyLate = e.AssignedTasks
                .Where(t => t.Status == TaskStatus.Completed)
                .Select(t => (double?)EF.Functions.DateDiffDay(
                    t.DueDate,
                    t.CompletionReport != null && t.CompletionReport.ReviewedAt.HasValue
                        ? t.CompletionReport.ReviewedAt.Value
                        : t.UpdatedAt))
                .Average() ?? 0,
            AverageTaskDuration = e.AssignedTasks
                .Where(t => t.Status == TaskStatus.Completed)
                .Select(t => (double?)EF.Functions.DateDiffDay(
                    t.StartDate,
                    t.CompletionReport != null && t.CompletionReport.ReviewedAt.HasValue
                        ? t.CompletionReport.ReviewedAt.Value
                        : t.UpdatedAt))
                .Average() ?? 0,
            AverageProgressUpdatesPerTask = e.ProgressLogs.Count(),
            TotalCompletionReportsSubmitted = e.AssignedTasks.Count(t =>
                t.CompletionReport != null && t.CompletionReport.UploadedByUserId == e.UserId),
            CurrentActiveTasks = e.AssignedTasks.Count(t => t.Status != TaskStatus.Completed)
        });
    }

    private static void FinalizePerformanceMetrics(IEnumerable<EngineerPerformanceDto> items)
    {
        foreach (var item in items)
        {
            item.CompletionRate = item.TotalTasksAssigned > 0
                ? Math.Round(item.TotalTasksCompleted * 100.0 / item.TotalTasksAssigned, 2)
                : 0;
            item.OnTimeCompletionRate = item.TotalTasksCompleted > 0
                ? Math.Round(item.TasksFinishedBeforeDeadline * 100.0 / item.TotalTasksCompleted, 2)
                : 0;
            item.LateRate = item.TotalTasksCompleted > 0
                ? Math.Round(item.TasksFinishedLate * 100.0 / item.TotalTasksCompleted, 2)
                : 0;
            item.AverageDaysEarlyLate = Math.Round(item.AverageDaysEarlyLate, 2);
            item.AverageTaskDuration = Math.Round(item.AverageTaskDuration, 2);
            item.AverageProgressUpdatesPerTask = item.TotalTasksAssigned > 0
                ? Math.Round(item.AverageProgressUpdatesPerTask / item.TotalTasksAssigned, 2)
                : 0;
            item.PerformanceScore = CalculatePerformanceScore(item);
            item.PerformanceTier = CalculatePerformanceTier(item.PerformanceScore);
        }
    }

    private static List<EngineerPerformanceDto> ApplyPerformanceSorting(
        IEnumerable<EngineerPerformanceDto> items,
        string? sortBy,
        bool descending)
    {
        var normalized = sortBy?.Trim().ToLowerInvariant();

        return normalized switch
        {
            "completedtasks" or "totaltaskscompleted" => descending
                ? items.OrderByDescending(x => x.TotalTasksCompleted).ThenBy(x => x.EngineerName).ToList()
                : items.OrderBy(x => x.TotalTasksCompleted).ThenBy(x => x.EngineerName).ToList(),
            "ontimerate" or "ontimecompletionrate" => descending
                ? items.OrderByDescending(x => x.OnTimeCompletionRate).ThenByDescending(x => x.PerformanceScore).ToList()
                : items.OrderBy(x => x.OnTimeCompletionRate).ThenByDescending(x => x.PerformanceScore).ToList(),
            "averagedelay" or "averagedaysearlylate" => descending
                ? items.OrderByDescending(x => x.AverageDaysEarlyLate).ThenByDescending(x => x.PerformanceScore).ToList()
                : items.OrderBy(x => x.AverageDaysEarlyLate).ThenByDescending(x => x.PerformanceScore).ToList(),
            "engineer" or "engineername" => descending
                ? items.OrderByDescending(x => x.EngineerName).ToList()
                : items.OrderBy(x => x.EngineerName).ToList(),
            _ => descending
                ? items.OrderBy(x => x.PerformanceScore).ThenBy(x => x.EngineerName).ToList()
                : items.OrderByDescending(x => x.PerformanceScore).ThenBy(x => x.EngineerName).ToList()
        };
    }

    private static double CalculatePerformanceScore(EngineerPerformanceDto dto)
    {
        var onTimeScore = dto.OnTimeCompletionRate;
        var completionScore = dto.CompletionRate;
        var delayScore = dto.TotalTasksCompleted == 0
            ? 0
            : dto.AverageDaysEarlyLate <= 0
                ? 100
                : Math.Max(0, 100 - dto.AverageDaysEarlyLate * 10);
        var progressScore = dto.TotalTasksAssigned == 0
            ? 0
            : Math.Min(100, dto.AverageProgressUpdatesPerTask / 3d * 100);
        var reportScore = dto.TotalTasksCompleted == 0
            ? 0
            : Math.Min(100, dto.TotalCompletionReportsSubmitted * 100.0 / dto.TotalTasksCompleted);

        return Math.Round(
            onTimeScore * 0.30 +
            completionScore * 0.25 +
            delayScore * 0.20 +
            progressScore * 0.15 +
            reportScore * 0.10,
            2);
    }

    private static WorkloadLevel CalculateWorkloadLevel(int activeTasks) =>
        activeTasks switch
        {
            <= 5 => WorkloadLevel.Low,
            <= 10 => WorkloadLevel.Medium,
            _ => WorkloadLevel.High
        };

    private static PerformanceTier CalculatePerformanceTier(double score) =>
        score switch
        {
            >= 90 => PerformanceTier.Excellent,
            >= 75 => PerformanceTier.Good,
            >= 60 => PerformanceTier.Average,
            _ => PerformanceTier.NeedsAttention
        };

    private static IQueryable<Engineer> ApplySorting(IQueryable<Engineer> query, string? sortBy, bool descending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "email" => descending
                ? query.OrderByDescending(e => e.User.Email)
                : query.OrderBy(e => e.User.Email),
            "hiredate" => descending
                ? query.OrderByDescending(e => e.HireDate)
                : query.OrderBy(e => e.HireDate),
            "position" => descending
                ? query.OrderByDescending(e => e.Position)
                : query.OrderBy(e => e.Position),
            _ => descending
                ? query.OrderByDescending(e => e.User.FullName)
                : query.OrderBy(e => e.User.FullName)
        };
    }
}
