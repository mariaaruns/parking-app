using ParkingApp.Application.DTOs.Admin;
using ParkingApp.Application.DTOs.SuperAdmin;

namespace ParkingApp.Application.Interfaces;

public interface ISuperAdminService
{
    Task<IEnumerable<BranchDto>> GetAllBranchesAsync();
    Task<BranchDto> CreateBranchAsync(CreateBranchDto dto);
    Task<BranchDto> UpdateBranchAsync(int branchId, UpdateBranchDto dto);
    Task<IEnumerable<CrossBranchSummaryDto>> GetCrossBranchSummaryAsync();
    Task<IEnumerable<AdminUserDto>> GetAllAdminsAsync();
    Task<AdminUserDto> CreateAdminAsync(CreateAdminUserDto dto);
    Task<AdminUserDto> ToggleAdminStatusAsync(int userId, bool isActive);
}
