using System.Security.Claims;
using ConstructionProjectTracker.API.DTOs.DeadlineExtensions;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionProjectTracker.API.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}")]
[Authorize]
public class ProjectDeadlineExtensionsController : ControllerBase
{
    private readonly IDeadlineExtensionService _service;

    public ProjectDeadlineExtensionsController(IDeadlineExtensionService service)
    {
        _service = service;
    }

    [HttpPost("deadline-extension-requests")]
    public async Task<ActionResult<DeadlineExtensionRequestDto>> CreateRequest(
        int projectId,
        [FromBody] CreateProjectDeadlineExtensionRequestDto dto)
    {
        try
        {
            var result = await _service.CreateProjectRequestAsync(projectId, dto, GetUserId());
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
    public async Task<IActionResult> GetLatest(int projectId)
    {
        try
        {
            var (userId, isAdmin) = GetUserContext();
            var result = await _service.GetLatestProjectRequestAsync(projectId, userId, isAdmin);
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
    public async Task<ActionResult<IReadOnlyList<ProjectDeadlineHistoryDto>>> GetHistory(int projectId)
    {
        try
        {
            var (userId, isAdmin) = GetUserContext();
            var result = await _service.GetProjectHistoryAsync(projectId, userId, isAdmin);
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

    [HttpPost("extend-deadline")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExtendDeadline(int projectId, [FromBody] AdminExtendProjectDeadlineDto dto)
    {
        try
        {
            await _service.ExtendProjectDeadlineAsync(projectId, dto, GetUserId());
            return Ok(new { message = "Project deadline extended successfully." });
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
