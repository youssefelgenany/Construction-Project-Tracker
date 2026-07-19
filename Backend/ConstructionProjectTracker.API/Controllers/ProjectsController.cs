using System.Security.Claims;
using ConstructionProjectTracker.API.DTOs.Common;
using ConstructionProjectTracker.API.DTOs.Predictions;
using ConstructionProjectTracker.API.DTOs.Projects;
using ConstructionProjectTracker.API.DTOs.Risks;
using ConstructionProjectTracker.API.DTOs.Tasks;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionProjectTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ITaskService _taskService;
    private readonly IRiskAnalysisService _riskAnalysisService;
    private readonly ITaskScheduleService _taskScheduleService;
    private readonly IProjectPredictionService _predictionService;

    public ProjectsController(
        IProjectService projectService,
        ITaskService taskService,
        IRiskAnalysisService riskAnalysisService,
        ITaskScheduleService taskScheduleService,
        IProjectPredictionService predictionService)
    {
        _projectService = projectService;
        _taskService = taskService;
        _riskAnalysisService = riskAnalysisService;
        _taskScheduleService = taskScheduleService;
        _predictionService = predictionService;
    }

    /// <summary>
    /// Returns a paginated list of projects with optional search and sorting.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProjectResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProjectResponseDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? sortBy,
        [FromQuery] bool descending = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _projectService.GetAllAsync(search, sortBy, descending, pageNumber, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Returns risk-aware projects with filters and sorting.
    /// </summary>
    [HttpGet("at-risk")]
    [ProducesResponseType(typeof(PagedResult<ProjectRiskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProjectRiskDto>>> GetAtRiskProjects(
        [FromQuery] string? search,
        [FromQuery] string? riskLevel,
        [FromQuery] string? sortBy,
        [FromQuery] bool descending = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100)
    {
        var (userId, isAdmin) = GetCurrentUserContext();
        var result = await _riskAnalysisService.GetProjectRisksAsync(
            userId,
            isAdmin,
            search,
            riskLevel,
            sortBy,
            descending,
            pageNumber,
            pageSize);

        return Ok(result);
    }

    /// <summary>
    /// Returns detailed project information including related entity counts.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProjectDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDetailsDto>> GetById(int id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project is null)
            return NotFound(new { message = $"Project with id {id} was not found." });

        return Ok(project);
    }

    /// <summary>
    /// Returns tasks for a project. Admins see all tasks; engineers see only their assigned tasks.
    /// </summary>
    [HttpGet("{id:int}/tasks")]
    [ProducesResponseType(typeof(PagedResult<TaskResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<TaskResponseDto>>> GetProjectTasks(
        int id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100)
    {
        var (userId, isAdmin) = GetCurrentUserContext();
        var result = await _taskService.GetProjectTasksAsync(id, userId, isAdmin, pageNumber, pageSize);

        if (result is null)
            return NotFound(new { message = $"Project with id {id} was not found." });

        return Ok(result);
    }

    /// <summary>
    /// Returns the deterministic delay prediction for a project based on execution velocity.
    /// </summary>
    [HttpGet("{id:int}/delay-prediction")]
    [ProducesResponseType(typeof(ProjectDelayPredictionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDelayPredictionDto>> GetDelayPrediction(int id)
    {
        var prediction = await _predictionService.GetProjectPredictionAsync(id);
        if (prediction is null)
            return NotFound(new { message = $"Project with id {id} was not found." });

        return Ok(prediction);
    }

    /// <summary>
    /// Returns the project timeline for Gantt-style visualization.
    /// </summary>
    [HttpGet("{id:int}/timeline")]
    [ProducesResponseType(typeof(ProjectTimelineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectTimelineDto>> GetTimeline(int id)
    {
        var (userId, isAdmin) = GetCurrentUserContext();
        var timeline = await _taskScheduleService.GetProjectTimelineAsync(id, userId, isAdmin);

        if (timeline is null)
            return NotFound(new { message = $"Project with id {id} was not found." });

        return Ok(timeline);
    }

    /// <summary>
    /// Returns tasks on the critical path in execution order.
    /// </summary>
    [HttpGet("{id:int}/critical-path")]
    [ProducesResponseType(typeof(IEnumerable<CriticalPathTaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CriticalPathTaskDto>>> GetCriticalPath(int id)
    {
        var (userId, isAdmin) = GetCurrentUserContext();
        var projectExists = await _projectService.GetByIdAsync(id);
        if (projectExists is null)
            return NotFound(new { message = $"Project with id {id} was not found." });

        var path = await _taskScheduleService.GetCriticalPathAsync(id, userId, isAdmin);
        return Ok(path);
    }

    /// <summary>
    /// Creates a new project. Admin only.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ProjectResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProjectResponseDto>> Create([FromBody] CreateProjectDto dto)
    {
        var project = await _projectService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }

    /// <summary>
    /// Updates an existing project. Admin only.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ProjectResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProjectResponseDto>> Update(int id, [FromBody] UpdateProjectDto dto)
    {
        var project = await _projectService.UpdateAsync(id, dto);
        if (project is null)
            return NotFound(new { message = $"Project with id {id} was not found." });

        return Ok(project);
    }

    /// <summary>
    /// Deletes a project and relies on cascade rules for related entities. Admin only.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _projectService.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { message = $"Project with id {id} was not found." });

        return NoContent();
    }

    private (int UserId, bool IsAdmin) GetCurrentUserContext()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.IsInRole("Admin");
        return (userId, isAdmin);
    }
}
