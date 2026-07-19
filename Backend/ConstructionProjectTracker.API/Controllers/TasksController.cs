using System.Security.Claims;
using ConstructionProjectTracker.API.DTOs.Common;
using ConstructionProjectTracker.API.DTOs.Risks;
using ConstructionProjectTracker.API.DTOs.Tasks;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionProjectTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ITaskCompletionReportService _completionReportService;
    private readonly IRiskAnalysisService _riskAnalysisService;
    private readonly ITaskDependencyService _taskDependencyService;

    public TasksController(
        ITaskService taskService,
        ITaskCompletionReportService completionReportService,
        IRiskAnalysisService riskAnalysisService,
        ITaskDependencyService taskDependencyService)
    {
        _taskService = taskService;
        _completionReportService = completionReportService;
        _riskAnalysisService = riskAnalysisService;
        _taskDependencyService = taskDependencyService;
    }

    /// <summary>
    /// Returns a paginated list of tasks with optional search, filters, and sorting. Admin only.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PagedResult<TaskResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TaskResponseDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? projectId,
        [FromQuery] int? engineerId,
        [FromQuery] string? priority,
        [FromQuery] string? status,
        [FromQuery] string? sortBy,
        [FromQuery] bool descending = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _taskService.GetAllAsync(
            search, projectId, engineerId, priority, status, sortBy, descending, pageNumber, pageSize);

        return Ok(result);
    }

    /// <summary>
    /// Returns tasks assigned to the authenticated engineer (resolved from JWT).
    /// </summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(PagedResult<TaskResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TaskResponseDto>>> GetMyTasks(
        [FromQuery] string? search,
        [FromQuery] string? priority,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100)
    {
        var (userId, _) = GetCurrentUserContext();
        var result = await _taskService.GetMyTasksAsync(
            userId, search, priority, status, pageNumber, pageSize);

        return Ok(result);
    }

    /// <summary>
    /// Returns risk-aware tasks with filters and sorting. Admins see all tasks; engineers see only their own.
    /// </summary>
    [HttpGet("at-risk")]
    [ProducesResponseType(typeof(PagedResult<TaskRiskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TaskRiskDto>>> GetAtRiskTasks(
        [FromQuery] string? search,
        [FromQuery] int? projectId,
        [FromQuery] int? engineerId,
        [FromQuery] string? priority,
        [FromQuery] string? status,
        [FromQuery] string? riskLevel,
        [FromQuery] string? sortBy,
        [FromQuery] bool descending = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100)
    {
        var (userId, isAdmin) = GetCurrentUserContext();
        var result = await _riskAnalysisService.GetTaskRisksAsync(
            userId,
            isAdmin,
            search,
            projectId,
            engineerId,
            priority,
            status,
            riskLevel,
            sortBy,
            descending,
            pageNumber,
            pageSize);

        return Ok(result);
    }

    /// <summary>
    /// Returns detailed task information including project and assigned engineer.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TaskDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDetailsDto>> GetById(int id)
    {
        var (userId, isAdmin) = GetCurrentUserContext();
        var task = await _taskService.GetByIdAsync(id, userId, isAdmin);
        if (task is null)
            return NotFound(new { message = $"Task with id {id} was not found." });

        return Ok(task);
    }

    /// <summary>
    /// Returns valid prerequisite candidates for a task (date-compatible, same project, no cycle).
    /// </summary>
    [HttpGet("{id:int}/valid-prerequisites")]
    [ProducesResponseType(typeof(IEnumerable<ValidPrerequisiteTaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ValidPrerequisiteTaskDto>>> GetValidPrerequisites(int id)
    {
        try
        {
            var (userId, isAdmin) = GetCurrentUserContext();
            var items = await _taskDependencyService.GetValidPrerequisitesAsync(id, userId, isAdmin);
            return Ok(items);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Returns dependencies for a task.
    /// </summary>
    [HttpGet("{id:int}/dependencies")]
    [ProducesResponseType(typeof(IEnumerable<TaskDependencyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<TaskDependencyDto>>> GetDependencies(int id)
    {
        try
        {
            var (userId, isAdmin) = GetCurrentUserContext();
            var dependencies = await _taskDependencyService.GetDependenciesAsync(id, userId, isAdmin);
            return Ok(dependencies);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Adds a dependency to a task. Admin only.
    /// </summary>
    [HttpPost("{id:int}/dependencies")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TaskDependencyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TaskDependencyDto>> AddDependency(int id, [FromBody] CreateTaskDependencyDto dto)
    {
        try
        {
            var (userId, isAdmin) = GetCurrentUserContext();
            var dependency = await _taskDependencyService.AddDependencyAsync(id, dto, userId, isAdmin);
            return CreatedAtAction(nameof(GetDependencies), new { id }, dependency);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Removes a dependency from a task. Admin only.
    /// </summary>
    [HttpDelete("{id:int}/dependencies/{dependsOnTaskId:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveDependency(int id, int dependsOnTaskId)
    {
        try
        {
            var (userId, isAdmin) = GetCurrentUserContext();
            var removed = await _taskDependencyService.RemoveDependencyAsync(id, dependsOnTaskId, userId, isAdmin);
            if (!removed)
                return NotFound(new { message = "Dependency was not found." });

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new task. Admin only.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TaskResponseDto>> Create([FromBody] CreateTaskDto dto)
    {
        try
        {
            var task = await _taskService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing task. Admins may update all fields; engineers may update status and progress only.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TaskResponseDto>> Update(int id, [FromBody] UpdateTaskDto dto)
    {
        try
        {
            var (userId, isAdmin) = GetCurrentUserContext();
            var task = await _taskService.UpdateAsync(id, dto, userId, isAdmin);
            if (task is null)
                return NotFound(new { message = $"Task with id {id} was not found." });

            return Ok(task);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a task and recalculates project progress. Admin only.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _taskService.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { message = $"Task with id {id} was not found." });

        return NoContent();
    }

    /// <summary>
    /// Uploads a completion report for a task. Assigned engineer only.
    /// </summary>
    [HttpPost("{id:int}/completion-report")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(TaskCompletionReportDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TaskCompletionReportDto>> UploadCompletionReport(
        int id,
        IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "File is required." });

        try
        {
            var (userId, _) = GetCurrentUserContext();
            var report = await _completionReportService.UploadAsync(id, file, userId);
            return CreatedAtAction(nameof(GetById), new { id }, report);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Downloads the completion report for a task.
    /// </summary>
    [HttpGet("{id:int}/completion-report/download")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadCompletionReport(int id)
    {
        try
        {
            var (userId, isAdmin) = GetCurrentUserContext();
            var result = await _completionReportService.DownloadAsync(id, userId, isAdmin);
            if (result is null)
                return NotFound(new { message = $"Completion report for task {id} was not found." });

            return result;
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Approves a pending completion report. Admin only.
    /// </summary>
    [HttpPost("{id:int}/completion-report/approve")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TaskResponseDto>> ApproveCompletionReport(int id)
    {
        try
        {
            var (userId, _) = GetCurrentUserContext();
            var task = await _completionReportService.ApproveAsync(id, userId);
            return Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Rejects a pending completion report. Admin only.
    /// </summary>
    [HttpPost("{id:int}/completion-report/reject")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TaskResponseDto>> RejectCompletionReport(
        int id,
        [FromBody] RejectCompletionReportDto dto)
    {
        try
        {
            var (userId, _) = GetCurrentUserContext();
            var task = await _completionReportService.RejectAsync(id, dto, userId);
            return Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private (int UserId, bool IsAdmin) GetCurrentUserContext()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.IsInRole("Admin");
        return (userId, isAdmin);
    }
}
