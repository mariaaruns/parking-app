using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingApp.Application.DTOs.Admin;
using ParkingApp.Application.DTOs.SuperAdmin;
using ParkingApp.Application.Interfaces;

namespace ParkingApp.API.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/superadmin")]
public class SuperAdminController(ISuperAdminService superAdminService) : ControllerBase
{
    // ---------- Branches ----------
    [HttpGet("branches")]
    public async Task<ActionResult<IEnumerable<BranchDto>>> GetBranches()
    {
        return Ok(await superAdminService.GetAllBranchesAsync());
    }

    [HttpPost("branches")]
    public async Task<ActionResult<BranchDto>> CreateBranch([FromBody] CreateBranchDto dto)
    {
        return Ok(await superAdminService.CreateBranchAsync(dto));
    }

    [HttpPut("branches/{id:int}")]
    public async Task<ActionResult<BranchDto>> UpdateBranch(int id, [FromBody] UpdateBranchDto dto)
    {
        return Ok(await superAdminService.UpdateBranchAsync(id, dto));
    }

    // ---------- Cross-branch comparison ----------
    [HttpGet("branches/summary")]
    public async Task<ActionResult<IEnumerable<CrossBranchSummaryDto>>> Summary()
    {
        return Ok(await superAdminService.GetCrossBranchSummaryAsync());
    }

    // ---------- Admin user management ----------
    [HttpGet("admins")]
    public async Task<ActionResult<IEnumerable<AdminUserDto>>> GetAdmins()
    {
        return Ok(await superAdminService.GetAllAdminsAsync());
    }

    [HttpPost("admins")]
    public async Task<ActionResult<AdminUserDto>> CreateAdmin([FromBody] CreateAdminUserDto dto)
    {
        return Ok(await superAdminService.CreateAdminAsync(dto));
    }

    [HttpPut("admins/{id:int}/status")]
    public async Task<ActionResult<AdminUserDto>> ToggleAdmin(int id, [FromBody] UpdateUserStatusDto dto)
    {
        return Ok(await superAdminService.ToggleAdminStatusAsync(id, dto.IsActive));
    }
}
