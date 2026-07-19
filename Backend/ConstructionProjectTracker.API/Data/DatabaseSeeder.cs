using ConstructionProjectTracker.API.Entities;
using ConstructionProjectTracker.API.Enums;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConstructionProjectTracker.API.Data;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedAdminIfNotExistsAsync(
            fullName: "Youssef Khaled",
            email: "youssefkhaled1204@gmail.com",
            password: "youssef12");
         await SeedAdminIfNotExistsAsync(
            fullName: "Youssef Elgenany",
            email: "youssef1204@gmail.com",
            password: "youssef12");

        await SeedEngineerIfNotExistsAsync(
            fullName: "Youssef Engineer",
            email: "youssef12@gmail.com",
            password: "youssef12",
            phoneNumber: "+201000000000",
            position: "Software Engineer",
            hireDate: new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    private async Task SeedAdminIfNotExistsAsync(string fullName, string email, string password)
    {
        if (await _context.Users.AnyAsync(u => u.Email == email))
        {
            _logger.LogInformation("Seed skipped — admin user {Email} already exists.", email);
            return;
        }

        var user = new User
        {
            FullName = fullName,
            Email = email,
            PasswordHash = _passwordHasher.HashPassword(password),
            Role = UserRole.Admin,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded admin user {Email}.", email);
    }

    private async Task SeedEngineerIfNotExistsAsync(
        string fullName,
        string email,
        string password,
        string phoneNumber,
        string position,
        DateTime hireDate)
    {
        var existingUser = await _context.Users
            .Include(u => u.Engineer)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (existingUser?.Engineer is not null)
        {
            _logger.LogInformation("Seed skipped — engineer {Email} already exists.", email);
            return;
        }

        if (existingUser is not null)
        {
            var repairedEngineer = new Engineer
            {
                UserId = existingUser.Id,
                PhoneNumber = phoneNumber,
                Position = position,
                HireDate = hireDate
            };

            _context.Engineers.Add(repairedEngineer);
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Repaired missing Engineer row for existing user {Email} (UserId={UserId}).",
                email,
                existingUser.Id);

            return;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = _passwordHasher.HashPassword(password),
                Role = UserRole.Engineer,
                IsActive = true
            };

            var engineer = new Engineer
            {
                User = user,
                PhoneNumber = phoneNumber,
                Position = position,
                HireDate = hireDate
            };

            _context.Engineers.Add(engineer);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Seeded engineer {Email} with UserId={UserId} and EngineerId={EngineerId}.",
                email,
                user.Id,
                engineer.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
