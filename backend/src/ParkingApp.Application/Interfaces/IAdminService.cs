using ParkingApp.Application.DTOs.Admin;

namespace ParkingApp.Application.Interfaces;

public interface IAdminService
{
    // Analytics
    Task<AnalyticsSummaryDto> GetAnalyticsAsync(int branchId);

    // Audit
    Task<IEnumerable<AuditLogDto>> GetAuditLogsForSessionAsync(int sessionId, int? branchScope);

    // Rate Cards
    Task<IEnumerable<RateCardDto>> GetRateCardsAsync(int branchId);
    Task<RateCardDto> CreateRateCardAsync(CreateRateCardDto dto);
    Task<RateCardDto> UpdateRateCardAsync(int rateCardId, UpdateRateCardDto dto, int? branchScope);
    Task DeleteRateCardAsync(int rateCardId, int? branchScope);

    // Zones
    Task<IEnumerable<ParkingZoneDto>> GetZonesAsync(int branchId);
    Task<ParkingZoneDto> CreateZoneAsync(CreateZoneDto dto);
    Task DeleteZoneAsync(int zoneId, int? branchScope);

    // Slots
    Task<IEnumerable<ParkingSlotDto>> GetSlotsAsync(int zoneId, int? branchScope);
    Task<ParkingSlotDto> CreateSlotAsync(CreateSlotDto dto, int? branchScope);
    Task<ParkingSlotDto> UpdateSlotStatusAsync(int slotId, UpdateSlotStatusDto dto, int? branchScope);
    Task DeleteSlotAsync(int slotId, int? branchScope);

    // Reconciliation
    Task<IEnumerable<StaffReconciliationDto>> GetShiftReconciliationAsync(int branchId, DateTime date);

    // Admin user management
    Task<IEnumerable<AdminUserDto>> GetAdminUsersAsync(int? branchId = null);
    Task<AdminUserDto> CreateAdminUserAsync(CreateAdminUserDto dto);
    Task<AdminUserDto> UpdateUserStatusAsync(int userId, UpdateUserStatusDto dto);
}
