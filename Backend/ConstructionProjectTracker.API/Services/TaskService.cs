using AutoMapper;

using ConstructionProjectTracker.API.Data;

using ConstructionProjectTracker.API.DTOs.Common;

using ConstructionProjectTracker.API.DTOs.Tasks;

using ConstructionProjectTracker.API.Entities;

using ConstructionProjectTracker.API.Enums;

using ConstructionProjectTracker.API.Interfaces;

using Microsoft.EntityFrameworkCore;



namespace ConstructionProjectTracker.API.Services;



public class TaskService : ITaskService

{

    private const int DefaultPageNumber = 1;

    private const int DefaultPageSize = 10;

    private const int MaxPageSize = 100;

    private const int EngineerMaxManualProgress = 90;



    private readonly ApplicationDbContext _context;

    private readonly IMapper _mapper;

    private readonly ITaskCompletionReportService _completionReportService;

    private readonly ITaskSchedulingValidationService _schedulingValidation;

    private readonly ILogger<TaskService> _logger;



    public TaskService(

        ApplicationDbContext context,

        IMapper mapper,

        ITaskCompletionReportService completionReportService,

        ITaskSchedulingValidationService schedulingValidation,

        ILogger<TaskService> logger)

    {

        _context = context;

        _mapper = mapper;

        _completionReportService = completionReportService;

        _schedulingValidation = schedulingValidation;

        _logger = logger;

    }



    public async Task<PagedResult<TaskResponseDto>> GetAllAsync(

        string? search,

        int? projectId,

        int? engineerId,

        string? priority,

        string? status,

        string? sortBy,

        bool descending,

        int pageNumber,

        int pageSize)

    {

        pageNumber = pageNumber < 1 ? DefaultPageNumber : pageNumber;

        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);



        var query = _context.Tasks

            .AsNoTracking()

            .Include(t => t.Project)

            .Include(t => t.AssignedEngineer!)

                .ThenInclude(e => e.User)

            .Include(t => t.CompletionReport!)

                .ThenInclude(r => r.UploadedByUser)

            .AsQueryable();



        if (!string.IsNullOrWhiteSpace(search))

        {

            var term = search.Trim().ToLower();

            query = query.Where(t =>

                t.Title.ToLower().Contains(term) ||

                t.Description.ToLower().Contains(term));

        }



        if (projectId.HasValue)

            query = query.Where(t => t.ProjectId == projectId.Value);



        if (engineerId.HasValue)

            query = query.Where(t => t.AssignedEngineerId == engineerId.Value);



        if (!string.IsNullOrWhiteSpace(priority) &&

            Enum.TryParse<TaskPriority>(priority, true, out var priorityFilter))

        {

            query = query.Where(t => t.Priority == priorityFilter);

        }



        if (!string.IsNullOrWhiteSpace(status) &&

            Enum.TryParse<ConstructionProjectTracker.API.Enums.TaskStatus>(status, true, out var statusFilter))

        {

            query = query.Where(t => t.Status == statusFilter);

        }



        query = ApplySorting(query, sortBy, descending);



        var totalCount = await query.CountAsync();



        var tasks = await query

            .Skip((pageNumber - 1) * pageSize)

            .Take(pageSize)

            .ToListAsync();



        var items = tasks.Select(MapToResponseDto).ToList();
        await EnrichWithDependencyInfoAsync(items);

        return new PagedResult<TaskResponseDto>

        {

            Items = items,

            PageNumber = pageNumber,

            PageSize = pageSize,

            TotalCount = totalCount,

            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)

        };

    }



    public async Task<PagedResult<TaskResponseDto>> GetMyTasksAsync(

        int userId,

        string? search,

        string? priority,

        string? status,

        int pageNumber,

        int pageSize)

    {

        var engineerId = await GetEngineerIdForUserAsync(userId);

        if (engineerId is null)

        {

            return new PagedResult<TaskResponseDto>

            {

                Items = Array.Empty<TaskResponseDto>(),

                PageNumber = pageNumber < 1 ? DefaultPageNumber : pageNumber,

                PageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize),

                TotalCount = 0,

                TotalPages = 0

            };

        }



        return await GetAllAsync(

            search, null, engineerId, priority, status, null, false, pageNumber, pageSize);

    }



    public async Task<PagedResult<TaskResponseDto>?> GetProjectTasksAsync(

        int projectId,

        int userId,

        bool isAdmin,

        int pageNumber,

        int pageSize)

    {

        if (!await _context.Projects.AnyAsync(p => p.Id == projectId))

            return null;



        pageNumber = pageNumber < 1 ? DefaultPageNumber : pageNumber;

        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);



        int? engineerIdFilter = null;



        if (!isAdmin)

        {

            var engineerId = await GetEngineerIdForUserAsync(userId);

            if (engineerId is null)

            {

                return new PagedResult<TaskResponseDto>

                {

                    Items = Array.Empty<TaskResponseDto>(),

                    PageNumber = pageNumber,

                    PageSize = pageSize,

                    TotalCount = 0,

                    TotalPages = 0

                };

            }



            var isAssignedToProject = await _context.ProjectAssignments.AnyAsync(pa =>

                pa.ProjectId == projectId && pa.EngineerId == engineerId.Value);



            if (!isAssignedToProject)

            {

                return new PagedResult<TaskResponseDto>

                {

                    Items = Array.Empty<TaskResponseDto>(),

                    PageNumber = pageNumber,

                    PageSize = pageSize,

                    TotalCount = 0,

                    TotalPages = 0

                };

            }



            engineerIdFilter = engineerId.Value;

        }



        return await GetAllAsync(

            null, projectId, engineerIdFilter, null, null, null, false, pageNumber, pageSize);

    }



    public async Task<TaskDetailsDto?> GetByIdAsync(int id, int userId, bool isAdmin)

    {

        var task = await _context.Tasks

            .AsNoTracking()

            .Include(t => t.Project)

            .Include(t => t.AssignedEngineer!)

                .ThenInclude(e => e.User)

            .Include(t => t.CompletionReport!)

                .ThenInclude(r => r.UploadedByUser)

            .FirstOrDefaultAsync(t => t.Id == id);



        if (task is null)

            return null;



        if (!await CanAccessTaskAsync(task, userId, isAdmin))

            return null;



        var dto = MapToDetailsDto(task);

        var dependencies = await _context.TaskDependencies
            .AsNoTracking()
            .Where(d => d.TaskId == id)
            .Select(d => new TaskDependencyDto
            {
                Id = d.Id,
                TaskId = d.TaskId,
                DependsOnTaskId = d.DependsOnTaskId,
                DependsOnTaskTitle = d.DependsOnTask.Title
            })
            .ToListAsync();

        dto.Dependencies = dependencies;

        if (dependencies.Count > 0)
        {
            var prerequisiteIds = dependencies.Select(d => d.DependsOnTaskId).ToList();
            dto.IncompletePrerequisites = await _context.Tasks
                .AsNoTracking()
                .Where(t => prerequisiteIds.Contains(t.Id) && t.Status != ConstructionProjectTracker.API.Enums.TaskStatus.Completed)
                .Select(t => new TaskPrerequisiteDto
                {
                    TaskId = t.Id,
                    Title = t.Title,
                    Status = t.Status,
                    IsComplete = false
                })
                .ToListAsync();
        }

        return dto;

    }



    public async Task<TaskResponseDto> CreateAsync(CreateTaskDto dto)

    {

        await ValidateEngineerAssignmentAsync(dto.ProjectId, dto.AssignedEngineerId);

        await _schedulingValidation.ValidateTaskDatesAgainstProjectAsync(dto.ProjectId, dto.StartDate, dto.DueDate);

        var task = _mapper.Map<TaskItem>(dto);

        task.CompletionPercentage = 0;

        task.Status = ConstructionProjectTracker.API.Enums.TaskStatus.NotStarted;

        task.UpdatedAt = DateTime.UtcNow;



        _context.Tasks.Add(task);

        await _context.SaveChangesAsync();



        _logger.LogInformation(

            "Task created: TaskId={TaskId}, ProjectId={ProjectId}, EngineerId={EngineerId}",

            task.Id, task.ProjectId, task.AssignedEngineerId);



        await RecalculateProjectProgressAsync(task.ProjectId);



        return (await MapToResponseDtoAsync(task.Id))!;

    }



    public async Task<TaskResponseDto?> UpdateAsync(int id, UpdateTaskDto dto, int userId, bool isAdmin)

    {

        var task = await _context.Tasks.FindAsync(id);

        if (task is null)

            return null;



        if (!await CanAccessTaskAsync(task, userId, isAdmin))

            throw new UnauthorizedAccessException("You do not have permission to update this task.");



        if (!isAdmin)

        {

            if (task.Status is ConstructionProjectTracker.API.Enums.TaskStatus.PendingReview

                or ConstructionProjectTracker.API.Enums.TaskStatus.Completed)

            {

                throw new InvalidOperationException("This task cannot be edited in its current state.");

            }



            if (dto.CompletionPercentage != task.CompletionPercentage)

            {

                throw new InvalidOperationException("Use the progress log to update task progress.");

            }



            if (dto.Status != task.Status)

            {

                throw new InvalidOperationException("Task status is updated automatically when progress changes.");

            }



            task.UpdatedAt = DateTime.UtcNow;

        }

        else

        {

            if (task.Status == ConstructionProjectTracker.API.Enums.TaskStatus.Completed)

                throw new InvalidOperationException("Completed tasks cannot be edited.");



            await _schedulingValidation.ValidateTaskDatesAgainstProjectAsync(task.ProjectId, dto.StartDate, dto.DueDate);



            if (dto.Status == ConstructionProjectTracker.API.Enums.TaskStatus.Completed)

            {

                throw new InvalidOperationException(

                    "Approve the completion report to mark this task as completed.");

            }



            if (dto.Status == ConstructionProjectTracker.API.Enums.TaskStatus.PendingReview)

            {

                throw new InvalidOperationException(

                    "Status Pending Review is set automatically when a completion report is submitted.");

            }



            if (dto.CompletionPercentage != task.CompletionPercentage)

            {

                throw new InvalidOperationException("Use the progress log to update task progress.");

            }



            if (dto.Status != task.Status)

            {

                throw new InvalidOperationException("Task status is updated automatically when progress changes.");

            }



            if (task.Status == ConstructionProjectTracker.API.Enums.TaskStatus.PendingReview)

            {

                task.Title = dto.Title;

                task.Description = dto.Description;

                task.Priority = dto.Priority;

                task.StartDate = dto.StartDate;

                task.DueDate = dto.DueDate;

            }

            else

            {

                _mapper.Map(dto, task);

            }



            task.UpdatedAt = DateTime.UtcNow;

        }



        await _context.SaveChangesAsync();



        _logger.LogInformation(

            "Task updated: TaskId={TaskId}, ProjectId={ProjectId}, EngineerId={EngineerId}",

            task.Id, task.ProjectId, task.AssignedEngineerId);



        await RecalculateProjectProgressAsync(task.ProjectId);



        return await MapToResponseDtoAsync(task.Id);

    }



    public async Task<bool> DeleteAsync(int id)

    {

        var task = await _context.Tasks

            .Include(t => t.CompletionReport)

            .FirstOrDefaultAsync(t => t.Id == id);



        if (task is null)

            return false;



        var projectId = task.ProjectId;

        var engineerId = task.AssignedEngineerId;



        if (task.CompletionReport is not null)

            await _completionReportService.DeletePhysicalFileAsync(task.CompletionReport.RelativeFilePath);



        _context.Tasks.Remove(task);

        await _context.SaveChangesAsync();



        _logger.LogInformation(

            "Task deleted: TaskId={TaskId}, ProjectId={ProjectId}, EngineerId={EngineerId}",

            id, projectId, engineerId);



        await RecalculateProjectProgressAsync(projectId);



        return true;

    }



    private async Task RecalculateProjectProgressAsync(int projectId)

    {

        var project = await _context.Projects.FindAsync(projectId);

        if (project is null)

            return;



        var tasks = await _context.Tasks

            .Where(t => t.ProjectId == projectId)

            .ToListAsync();



        if (tasks.Count == 0)

        {

            project.ProgressPercentage = 0;

            project.Status = ProjectStatus.NotStarted;

        }

        else

        {

            project.ProgressPercentage = (int)Math.Round(

                tasks.Average(t => t.CompletionPercentage),

                MidpointRounding.AwayFromZero);



            project.Status = tasks.All(t => t.Status == ConstructionProjectTracker.API.Enums.TaskStatus.Completed)

                ? ProjectStatus.Completed

                : project.ProgressPercentage > 0

                    ? ProjectStatus.InProgress

                    : ProjectStatus.NotStarted;

        }



        await _context.SaveChangesAsync();



        _logger.LogInformation(

            "Project progress recalculated: ProjectId={ProjectId}, Progress={Progress}, Status={Status}",

            projectId, project.ProgressPercentage, project.Status);

    }



    private async Task ValidateEngineerAssignmentAsync(int projectId, int engineerId)

    {

        if (!await _context.Projects.AnyAsync(p => p.Id == projectId))

            throw new InvalidOperationException("Project does not exist.");



        if (!await _context.Engineers.AnyAsync(e => e.Id == engineerId))

            throw new InvalidOperationException("Engineer does not exist.");



        var isAssigned = await _context.ProjectAssignments.AnyAsync(pa =>

            pa.ProjectId == projectId && pa.EngineerId == engineerId);



        if (!isAssigned)

            throw new InvalidOperationException("Engineer is not assigned to this project.");

    }



    private async Task<int?> GetEngineerIdForUserAsync(int userId) =>

        await _context.Engineers

            .AsNoTracking()

            .Where(e => e.UserId == userId)

            .Select(e => (int?)e.Id)

            .FirstOrDefaultAsync();



    private async Task<bool> CanAccessTaskAsync(TaskItem task, int userId, bool isAdmin)

    {

        if (isAdmin)

            return true;



        var engineerId = await GetEngineerIdForUserAsync(userId);

        return engineerId.HasValue && task.AssignedEngineerId == engineerId.Value;

    }



    private async Task<TaskResponseDto?> MapToResponseDtoAsync(int taskId)

    {

        var task = await _context.Tasks

            .AsNoTracking()

            .Include(t => t.Project)

            .Include(t => t.AssignedEngineer!)

                .ThenInclude(e => e.User)

            .Include(t => t.CompletionReport!)

                .ThenInclude(r => r.UploadedByUser)

            .FirstOrDefaultAsync(t => t.Id == taskId);



        return task is null ? null : MapToResponseDto(task);

    }



    private TaskResponseDto MapToResponseDto(TaskItem task)

    {

        var dto = _mapper.Map<TaskResponseDto>(task);

        dto.CompletionReport = task.CompletionReport is null

            ? null

            : MapCompletionReportDto(task.CompletionReport);

        return dto;

    }



    private TaskDetailsDto MapToDetailsDto(TaskItem task)

    {

        var dto = _mapper.Map<TaskDetailsDto>(task);

        dto.CompletionReport = task.CompletionReport is null

            ? null

            : MapCompletionReportDto(task.CompletionReport);

        return dto;

    }



    private static TaskCompletionReportDto MapCompletionReportDto(TaskCompletionReport report) =>

        new()

        {

            Id = report.Id,

            TaskId = report.TaskId,

            OriginalFileName = report.OriginalFileName,

            Extension = report.Extension,

            ContentType = report.ContentType,

            FileSize = report.FileSize,

            UploadedAt = report.UploadedAt,

            UploadedBy = report.UploadedByUser.FullName,

            RejectionComment = report.RejectionComment

        };



    private async Task EnrichWithDependencyInfoAsync(List<TaskResponseDto> items)
    {
        if (items.Count == 0)
            return;

        var taskIds = items.Select(item => item.Id).ToList();
        var dependencies = await _context.TaskDependencies
            .AsNoTracking()
            .Where(d => taskIds.Contains(d.TaskId))
            .ToListAsync();

        if (dependencies.Count == 0)
            return;

        var prerequisiteIds = dependencies.Select(d => d.DependsOnTaskId).Distinct().ToList();
        var prerequisites = await _context.Tasks
            .AsNoTracking()
            .Where(t => prerequisiteIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id);

        foreach (var item in items)
        {
            var taskDependencies = dependencies.Where(d => d.TaskId == item.Id).ToList();
            item.DependencyCount = taskDependencies.Count;
            item.IncompletePrerequisites = taskDependencies
                .Where(d =>
                    prerequisites.TryGetValue(d.DependsOnTaskId, out var prerequisite) &&
                    prerequisite.Status != ConstructionProjectTracker.API.Enums.TaskStatus.Completed)
                .Select(d => new TaskPrerequisiteDto
                {
                    TaskId = d.DependsOnTaskId,
                    Title = prerequisites[d.DependsOnTaskId].Title,
                    Status = prerequisites[d.DependsOnTaskId].Status,
                    IsComplete = false
                })
                .ToList();
        }
    }



    private static IQueryable<TaskItem> ApplySorting(IQueryable<TaskItem> query, string? sortBy, bool descending)

    {

        return sortBy?.ToLowerInvariant() switch

        {

            "startdate" => descending

                ? query.OrderByDescending(t => t.StartDate)

                : query.OrderBy(t => t.StartDate),

            "duedate" => descending

                ? query.OrderByDescending(t => t.DueDate)

                : query.OrderBy(t => t.DueDate),

            "priority" => descending

                ? query.OrderByDescending(t => t.Priority)

                : query.OrderBy(t => t.Priority),

            "completionpercentage" => descending

                ? query.OrderByDescending(t => t.CompletionPercentage)

                : query.OrderBy(t => t.CompletionPercentage),

            _ => descending

                ? query.OrderByDescending(t => t.Title)

                : query.OrderBy(t => t.Title)

        };

    }

}

