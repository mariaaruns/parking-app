namespace ParkingApp.Application.DTOs.Auth;

public record LoginRequestDto(string Email, string Password);

public record LoginResponseDto(
    string Token,
    int UserId,
    string FullName,
    string Role,
    int? BranchId,
    string? BranchName,
    DateTime ExpiresAt
);
