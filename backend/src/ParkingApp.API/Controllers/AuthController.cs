using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingApp.API.Extensions;
using ParkingApp.Application.DTOs.Auth;
using ParkingApp.Application.Interfaces;

namespace ParkingApp.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Sign in and receive a JWT containing UserId, Role, BranchId and FullName.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { detail = "Email and password are required." });

        try
        {
            return Ok(await authService.LoginAsync(request));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { detail = ex.Message });
        }
    }

    /// <summary>Returns the identity encoded in the current token.</summary>
    [Authorize]
    [HttpGet("me")]
    public ActionResult Me()
    {
        var user = HttpContext.User;
        return Ok(new
        {
            userId = user.GetUserId(),
            fullName = user.FindFirst("FullName")?.Value,
            role = user.GetRole().ToString(),
            branchId = user.GetBranchIdClaim()
        });
    }
}
