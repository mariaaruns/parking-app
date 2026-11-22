using Microsoft.EntityFrameworkCore;
using ParkingApp.Application.DTOs.Auth;
using ParkingApp.Application.Interfaces;
using ParkingApp.Infrastructure.Persistence;

namespace ParkingApp.Infrastructure.Services;

public class AuthService(AppDbContext context, IJwtTokenService jwtTokenService) : IAuthService
{
    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await context.AppUsers
            .Include(u => u.Branch)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower() && u.IsActive);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var token = jwtTokenService.GenerateToken(user, user.Branch?.Name);
      
        return new LoginResponseDto(
            Token: token,
            UserId: user.Id,
            FullName: user.FullName,
            Role: user.Role.ToString(),
            BranchId: user.BranchId,
            BranchName: user.Branch?.Name,
            ExpiresAt: jwtTokenService.GetExpiration()
        );

    }
}
