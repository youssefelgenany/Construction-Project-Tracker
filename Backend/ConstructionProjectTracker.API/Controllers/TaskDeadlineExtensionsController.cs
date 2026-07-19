using System.Security.Claims;
using ConstructionProjectTracker.API.DTOs.DeadlineExtensions;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionProjectTracker.API.Controllers;

[ApiController]
[Route("api/tasks/{taskId:int}")]
[Authorize]
public class TaskDeadlineExtensionsController : ControllerBase
{
    private readonly IDeadlineExtensionService _service;
    private readonly ITaskDeadlineCascadeService _cascadeService;

    public TaskDeadlineExtensionsController(
        IDeadlineExtensionService service,
        ITaskDeadlineCascadeService cascadeService)
    {
        _service = service;
        _cascadeService = cascadeService;
    }

    [HttpPost("deadline-extension-requests")]
    public async Task<ActionResult<DeadlineExtensionRequestDto>> CreateRequest(
        int taskId,
        [FromBody] CreateTaskDeadlineExtensionRequestDto dto)
    {
        try
        {
            var result = await _service.CreateTaskRequestAsync(taskId, dto, GetUserId());
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("deadline-extension-requests/latest")]
    public async Task<IActionResult> GetLatest(int taskId)
    {
        try
        {
            var (userId, isAdmin) = GetUserContext();
            var result = await _service.GetLatestTaskRequestAsync(taskId, userId, isAdmin);
            return new ObjectResult(result) { StatusCode = StatusCodes.Status200OK };
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("deadline-history")]
    public async Task<ActionResult<IReadOnlyList<TaskDeadlineHistoryDto>>> GetHistory(int taskId)
    {
        try
        {
            var (userId, isAdmin) = GetUserContext();
            var result = await _service.GetTaskHistoryAsync(taskId, userId, isAdmin);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("extend-deadline/analyze")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ScheduleImpactAnalysisDto>> AnalyzeExtension(
        int taskId,
        [FromBody] AnalyzeTaskDeadlineExtensionDto dto)
    {
        try
        {
            var result = await _cascadeService.AnalyzeAsync(taskId, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("extend-deadline/apply")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApplyTaskDeadlineExtensionResultDto>> ApplyExtension(
        int taskId,
        [FromBody] ApplyTaskDeadlineExtensionDto dto)
    {
        try
        {
            var result = await _cascadeService.ApplyAsync(taskId, dto, GetUserId());
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("extend-deadline")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExtendDeadline(int taskId, [FromBody] AdminExtendTaskDeadlineDto dto)
    {
        try
        {
            // Backward-compatible entry: analyze then apply when no confirmation is needed.
            var analysis = await _cascadeService.AnalyzeAsync(taskId, new AnalyzeTaskDeadlineExtensionDto
            {
                NewDueDate = dto.NewDueDate,
                Reason = dto.Reason
            });

            if (analysis.HasConflicts)
            {
                return Conflict(new
                {
                    message = "This extension conflicts with dependent tasks or the project end date.",
                    requiresConfirmation = true,
                    analysis
                });
            }

            var result = await _cascadeService.ApplyAsync(taskId, new ApplyTaskDeadlineExtensionDto
            {
                NewDueDate = dto.NewDueDate,
                Reason = dto.Reason,
                ConfirmProjectExtension = false
            }, GetUserId());

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private (int UserId, bool IsAdmin) GetUserContext()
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole("Admin");
        return (userId, isAdmin);
    }
}
