using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingApp.API.Extensions;
using ParkingApp.Application.DTOs.Parking;
using ParkingApp.Application.Interfaces;
using ParkingApp.Domain.Enums;

namespace ParkingApp.API.Controllers;

[ApiController]
[Authorize(Roles = "Staff,Admin,SuperAdmin")]
[Route("api/parking")]
public class ParkingController(IParkingService parkingService, IConfiguration configuration) : ControllerBase
{
    /// <summary>Live slot status grouped by vehicle type and zone.</summary>
    [HttpGet("slots")]
    public async Task<ActionResult<BranchSlotStatusDto>> GetSlots([FromQuery] int? branchId)
    {
        var resolved = User.ResolveBranchId(branchId);
        return Ok(await parkingService.GetSlotStatusAsync(resolved));
    }

    /// <summary>Headline capacity for Bikes, Cars and Heavy Vehicles.</summary>
    [HttpGet("capacity")]
    public async Task<ActionResult<CapacityStripDto>> GetCapacity([FromQuery] int? branchId)
    {
        var resolved = User.ResolveBranchId(branchId);
        return Ok(await parkingService.GetCapacityStripAsync(resolved));
    }

    /// <summary>Check a vehicle in, occupy a slot and issue a public QR ticket.</summary>
    [HttpPost("check-in")]
    public async Task<ActionResult<CheckInResponseDto>> CheckIn([FromBody] CheckInRequestDto request)
    {
        var branchId = User.ResolveBranchId(request.BranchId);
        var scoped = request with { BranchId = branchId };
        var result = await parkingService.CheckInAsync(scoped, User.GetUserId());

        var frontendBase = configuration["Frontend:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        var publicUrl = $"{frontendBase.TrimEnd('/')}/p/{result.PublicToken}";
        return Ok(result with { TicketUrl = publicUrl });
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<IEnumerable<ParkingSessionDto>>> GetSessions(
        [FromQuery] int? branchId,
        [FromQuery] SessionStatus? status,
        [FromQuery] bool includeDeleted = false)
    {
        var resolved = User.ResolveBranchId(branchId);
        var canSeeDeleted = User.GetRole() is UserRole.Admin or UserRole.SuperAdmin;
        return Ok(await parkingService.GetSessionsAsync(resolved, status, includeDeleted && canSeeDeleted));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ParkingSessionDto>> GetSession(int id)
    {
        return Ok(await parkingService.GetSessionAsync(id, User.GetUserId(), User.GetRole(), User.BranchScope()));
    }

    [HttpGet("{id:int}/live-rent")]
    public async Task<ActionResult<RentPreviewDto>> GetLiveRent(int id)
    {
        return Ok(await parkingService.GetLiveRentAsync(id, User.BranchScope()));
    }

    /// <summary>Edit vehicle/slot details. Staff receive 403 for completed sessions.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ParkingSessionDto>> Edit(int id, [FromBody] EditSessionRequestDto request)
    {
        return Ok(await parkingService.EditSessionAsync(id, request, User.GetUserId(), User.GetRole(), User.BranchScope()));
    }

    /// <summary>Finalize rent, mark paid/completed and free the slot.</summary>
    [HttpPost("{id:int}/checkout")]
    public async Task<ActionResult<CheckoutResponseDto>> Checkout(int id, [FromBody] CheckoutRequestDto request)
    {
        return Ok(await parkingService.CheckoutAsync(id, request, User.GetUserId(), User.BranchScope()));
    }

    /// <summary>Soft delete (Admin/SuperAdmin only) with a mandatory reason.</summary>
    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> SoftDelete(int id, [FromBody] SoftDeleteRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request?.Reason))
            return BadRequest(new { detail = "A deletion reason is required." });

        await parkingService.SoftDeleteAsync(id, request, User.GetUserId(), User.BranchScope());
        return NoContent();
    }
}
