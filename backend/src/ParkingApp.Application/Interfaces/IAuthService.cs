using ParkingApp.Application.DTOs.Auth;

namespace ParkingApp.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
}
