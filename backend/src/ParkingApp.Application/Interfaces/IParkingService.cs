using ParkingApp.Application.DTOs.Parking;
using ParkingApp.Domain.Enums;

namespace ParkingApp.Application.Interfaces;

public interface IParkingService
{
    Task<BranchSlotStatusDto> GetSlotStatusAsync(int branchId);
    Task<CapacityStripDto> GetCapacityStripAsync(int branchId);
    Task<CheckInResponseDto> CheckInAsync(CheckInRequestDto request, int userId);
    Task<ParkingSessionDto> GetSessionAsync(int sessionId, int userId, UserRole role, int? branchScope);
    Task<IEnumerable<ParkingSessionDto>> GetSessionsAsync(int branchId, SessionStatus? status, bool includeDeleted = false);
    Task<ParkingSessionDto> EditSessionAsync(int sessionId, EditSessionRequestDto request, int userId, UserRole role, int? branchScope);
    Task<CheckoutResponseDto> CheckoutAsync(int sessionId, CheckoutRequestDto request, int userId, int? branchScope);
    Task SoftDeleteAsync(int sessionId, SoftDeleteRequestDto request, int userId, int? branchScope);
    Task<RentPreviewDto> GetLiveRentAsync(int sessionId, int? branchScope);
}
