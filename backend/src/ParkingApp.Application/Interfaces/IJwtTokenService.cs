using ParkingApp.Domain.Entities;

namespace ParkingApp.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(AppUser user, string? branchName);
    DateTime GetExpiration();
}
