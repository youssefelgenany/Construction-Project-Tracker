using System.Security.Claims;
using ConstructionProjectTracker.API.DTOs.Auth;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionProjectTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);
        if (result is null)
            return Unauthorized(new { message = "Invalid email or password." });

        return Ok(result);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { message = "Invalid token." });

        var result = await _authService.ChangePasswordAsync(userId.Value, request);

        return result switch
        {
            ChangePasswordResult.Success => Ok(new { message = "Password changed successfully." }),
            ChangePasswordResult.InvalidCurrentPassword => Unauthorized(new { message = "Current password is incorrect." }),
            ChangePasswordResult.UserNotFound => NotFound(new { message = "User not found." }),
            _ => BadRequest()
        };
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(new { message = "Invalid token." });

        var user = await _authService.GetCurrentUserAsync(userId.Value);
        if (user is null)
            return NotFound(new { message = "User not found." });

        return Ok(user);
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var userId) ? userId : null;
    }
}
