using ConstructionProjectTracker.API.DTOs.Auth;

namespace ConstructionProjectTracker.API.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
    Task<ChangePasswordResult> ChangePasswordAsync(int userId, ChangePasswordDto request);
    Task<UserDto?> GetCurrentUserAsync(int userId);
}
