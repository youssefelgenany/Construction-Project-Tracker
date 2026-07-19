using System.Security.Claims;
using ConstructionProjectTracker.API.Entities;

namespace ConstructionProjectTracker.API.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
    DateTime GetTokenExpiration();
    IEnumerable<Claim> GenerateClaims(User user);
}
