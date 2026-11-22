using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ParkingApp.Application.Interfaces;
using ParkingApp.Domain.Entities;

namespace ParkingApp.Infrastructure.Services;

public class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    private readonly string _secret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
    private readonly string _issuer = configuration["Jwt:Issuer"] ?? "ParkingApp";
    private readonly string _audience = configuration["Jwt:Audience"] ?? "ParkingApp";
    private readonly int _expiryMinutes = int.Parse(configuration["Jwt:ExpiryMinutes"] ?? "480");

    public string GenerateToken(AppUser user, string? branchName)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("BranchId", user.BranchId?.ToString() ?? ""),
            new Claim("BranchName", branchName ?? ""),
            new Claim("FullName", user.FullName)
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: GetExpiration(),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public DateTime GetExpiration() => DateTime.UtcNow.AddMinutes(_expiryMinutes);
}
