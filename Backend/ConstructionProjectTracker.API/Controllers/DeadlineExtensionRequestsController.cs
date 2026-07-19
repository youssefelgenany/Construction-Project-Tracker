using System.Security.Claims;
using ConstructionProjectTracker.API.DTOs.DeadlineExtensions;
using ConstructionProjectTracker.API.Enums;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionProjectTracker.API.Controllers;

[ApiController]
[Route("api/deadline-extension-requests")]
[Authorize]
public class DeadlineExtensionRequestsController : ControllerBase
{
    private readonly IDeadlineExtensionService _service;

    public DeadlineExtensionRequestsController(IDeadlineExtensionService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<DeadlineExtensionRequestDto>>> GetAll(
        [FromQuery] ExtensionRequestStatus? status)
    {
        var items = await _service.GetAdminRequestsAsync(status);
        return Ok(items);
    }

    [HttpPost("tasks/{requestId:int}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DeadlineExtensionRequestDto>> ApproveTask(
        int requestId,
        [FromBody] ReviewDeadlineExtensionDto dto)
    {
        try
        {
            var result = await _service.ApproveTaskRequestAsync(requestId, dto ?? new(), GetUserId());
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("tasks/{requestId:int}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DeadlineExtensionRequestDto>> RejectTask(
        int requestId,
        [FromBody] ReviewDeadlineExtensionDto dto)
    {
        try
        {
            var result = await _service.RejectTaskRequestAsync(requestId, dto ?? new(), GetUserId());
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

    [HttpPost("projects/{requestId:int}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DeadlineExtensionRequestDto>> ApproveProject(
        int requestId,
        [FromBody] ReviewDeadlineExtensionDto dto)
    {
        try
        {
            var result = await _service.ApproveProjectRequestAsync(requestId, dto ?? new(), GetUserId());
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("projects/{requestId:int}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DeadlineExtensionRequestDto>> RejectProject(
        int requestId,
        [FromBody] ReviewDeadlineExtensionDto dto)
    {
        try
        {
            var result = await _service.RejectProjectRequestAsync(requestId, dto ?? new(), GetUserId());
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
}
