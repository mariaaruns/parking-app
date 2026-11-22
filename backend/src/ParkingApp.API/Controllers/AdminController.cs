using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingApp.API.Extensions;
using ParkingApp.Application.DTOs.Admin;
using ParkingApp.Application.Interfaces;

namespace ParkingApp.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/admin")]
public class AdminController(IAdminService adminService) : ControllerBase
{
    // ---------- Analytics ----------
    [HttpGet("analytics")]
    public async Task<ActionResult<AnalyticsSummaryDto>> Analytics([FromQuery] int? branchId)
    {
        return Ok(await adminService.GetAnalyticsAsync(User.ResolveBranchId(branchId)));
    }

    // ---------- Audit history ----------
    [HttpGet("audit/session/{sessionId:int}")]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> SessionAudit(int sessionId)
    {
        return Ok(await adminService.GetAuditLogsForSessionAsync(sessionId, User.BranchScope()));
    }

    // ---------- Rate cards ----------
    [HttpGet("ratecards")]
    public async Task<ActionResult<IEnumerable<RateCardDto>>> GetRateCards([FromQuery] int? branchId)
    {
        return Ok(await adminService.GetRateCardsAsync(User.ResolveBranchId(branchId)));
    }

    [HttpPost("ratecards")]
    public async Task<ActionResult<RateCardDto>> CreateRateCard([FromBody] CreateRateCardDto dto)
    {
        var scoped = dto with { BranchId = User.ResolveBranchId(dto.BranchId) };
        return Ok(await adminService.CreateRateCardAsync(scoped));
    }

    [HttpPut("ratecards/{id:int}")]
    public async Task<ActionResult<RateCardDto>> UpdateRateCard(int id, [FromBody] UpdateRateCardDto dto)
    {
        return Ok(await adminService.UpdateRateCardAsync(id, dto, User.BranchScope()));
    }

    [HttpDelete("ratecards/{id:int}")]
    public async Task<IActionResult> DeleteRateCard(int id)
    {
        await adminService.DeleteRateCardAsync(id, User.BranchScope());
        return NoContent();
    }

    // ---------- Zones ----------
    [HttpGet("zones")]
    public async Task<ActionResult<IEnumerable<ParkingZoneDto>>> GetZones([FromQuery] int? branchId)
    {
        return Ok(await adminService.GetZonesAsync(User.ResolveBranchId(branchId)));
    }

    [HttpPost("zones")]
    public async Task<ActionResult<ParkingZoneDto>> CreateZone([FromBody] CreateZoneDto dto)
    {
        var scoped = dto with { BranchId = User.ResolveBranchId(dto.BranchId) };
        return Ok(await adminService.CreateZoneAsync(scoped));
    }

    [HttpDelete("zones/{id:int}")]
    public async Task<IActionResult> DeleteZone(int id)
    {
        await adminService.DeleteZoneAsync(id, User.BranchScope());
        return NoContent();
    }

    // ---------- Slots ----------
    [HttpGet("zones/{zoneId:int}/slots")]
    public async Task<ActionResult<IEnumerable<ParkingSlotDto>>> GetSlots(int zoneId)
    {
        return Ok(await adminService.GetSlotsAsync(zoneId, User.BranchScope()));
    }

    [HttpPost("slots")]
    public async Task<ActionResult<ParkingSlotDto>> CreateSlot([FromBody] CreateSlotDto dto)
    {
        return Ok(await adminService.CreateSlotAsync(dto, User.BranchScope()));
    }

    [HttpPut("slots/{id:int}/status")]
    public async Task<ActionResult<ParkingSlotDto>> UpdateSlotStatus(int id, [FromBody] UpdateSlotStatusDto dto)
    {
        return Ok(await adminService.UpdateSlotStatusAsync(id, dto, User.BranchScope()));
    }

    [HttpDelete("slots/{id:int}")]
    public async Task<IActionResult> DeleteSlot(int id)
    {
        await adminService.DeleteSlotAsync(id, User.BranchScope());
        return NoContent();
    }

    // ---------- Staff shift reconciliation ----------
    [HttpGet("reconciliation")]
    public async Task<ActionResult<IEnumerable<StaffReconciliationDto>>> Reconciliation(
        [FromQuery] int? branchId, [FromQuery] DateTime? date)
    {
        return Ok(await adminService.GetShiftReconciliationAsync(
            User.ResolveBranchId(branchId), date ?? DateTime.UtcNow));
    }

    // ---------- User management (SuperAdmin only) ----------
    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<AdminUserDto>>> GetUsers([FromQuery] int? branchId)
    {
        return Ok(await adminService.GetAdminUsersAsync(branchId));
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("users")]
    public async Task<ActionResult<AdminUserDto>> CreateUser([FromBody] CreateAdminUserDto dto)
    {
        return Ok(await adminService.CreateAdminUserAsync(dto));
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPut("users/{id:int}/status")]
    public async Task<ActionResult<AdminUserDto>> UpdateUserStatus(int id, [FromBody] UpdateUserStatusDto dto)
    {
        return Ok(await adminService.UpdateUserStatusAsync(id, dto));
    }
}
