using System.Security.Claims;
using ConstructionProjectTracker.API.DTOs.Tasks;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionProjectTracker.API.Controllers;

[ApiController]
[Route("api/tasks/{taskId:int}/progress-log")]
[Authorize]
public class TaskProgressLogsController : ControllerBase
{
    private readonly ITaskProgressLogService _progressLogService;

    public TaskProgressLogsController(ITaskProgressLogService progressLogService)
    {
        _progressLogService = progressLogService;
    }

    /// <summary>
    /// Returns progress history for a task, newest first.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaskProgressLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TaskProgressLogDto>>> GetByTaskId(int taskId)
    {
        try
        {
            var (userId, isAdmin) = GetCurrentUserContext();
            var logs = await _progressLogService.GetByTaskIdAsync(taskId, userId, isAdmin);
            return Ok(logs);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Records a progress update with description and updates task completion.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TaskProgressLogDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TaskProgressLogDto>> Create(int taskId, [FromBody] CreateTaskProgressLogDto dto)
    {
        try
        {
            var (userId, isAdmin) = GetCurrentUserContext();
            var log = await _progressLogService.CreateAsync(taskId, dto, userId, isAdmin);
            return CreatedAtAction(nameof(GetByTaskId), new { taskId }, log);
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

    private (int UserId, bool IsAdmin) GetCurrentUserContext()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.IsInRole("Admin");
        return (userId, isAdmin);
    }
}
