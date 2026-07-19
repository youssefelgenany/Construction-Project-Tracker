using AutoMapper;
using ConstructionProjectTracker.API.Data;
using ConstructionProjectTracker.API.DTOs.Auth;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConstructionProjectTracker.API.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;

    public AuthService(
        ApplicationDbContext context,
        IJwtService jwtService,
        IPasswordHasher passwordHasher,
        IMapper mapper)
    {
        _context = context;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return null;

        return new LoginResponseDto
        {
            Token = _jwtService.GenerateToken(user),
            Expiration = _jwtService.GetTokenExpiration(),
            User = _mapper.Map<UserDto>(user)
        };
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(int userId, ChangePasswordDto request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null)
            return ChangePasswordResult.UserNotFound;

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return ChangePasswordResult.InvalidCurrentPassword;

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();

        return ChangePasswordResult.Success;
    }

    public async Task<UserDto?> GetCurrentUserAsync(int userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        return user is null ? null : _mapper.Map<UserDto>(user);
    }
}
