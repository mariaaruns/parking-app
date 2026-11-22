using ParkingApp.Domain.Enums;

namespace ParkingApp.Application.DTOs.Admin;

// --- Rate Card ---
public record RateCardDto(
    int Id,
    int BranchId,
    VehicleType VehicleType,
    int GracePeriodMinutes,
    decimal BaseHours,
    decimal BaseRate,
    decimal AdditionalHourlyRate,
    decimal DailyMaxCap
);

public record UpdateRateCardDto(
    int GracePeriodMinutes,
    decimal BaseHours,
    decimal BaseRate,
    decimal AdditionalHourlyRate,
    decimal DailyMaxCap
);

public record CreateRateCardDto(
    int BranchId,
    VehicleType VehicleType,
    int GracePeriodMinutes,
    decimal BaseHours,
    decimal BaseRate,
    decimal AdditionalHourlyRate,
    decimal DailyMaxCap
);

// --- Zone ---
public record ParkingZoneDto(int Id, int BranchId, string Name, VehicleType VehicleType, int TotalSlots);
public record CreateZoneDto(int BranchId, string Name, VehicleType VehicleType);
public record UpdateZoneDto(string Name);

// --- Slot ---
public record ParkingSlotDto(int Id, int ZoneId, string SlotCode, SlotStatus Status);
public record CreateSlotDto(int ZoneId, string SlotCode);
public record UpdateSlotStatusDto(SlotStatus Status);

// --- Audit Log ---
public record AuditLogDto(
    int Id,
    int ParkingSessionId,
    string EditedBy,
    AuditAction Action,
    string OldValuesJson,
    string NewValuesJson,
    string Reason,
    DateTime Timestamp
);

// --- Analytics ---
public record RevenueByVehicleTypeDto(VehicleType VehicleType, decimal TotalRevenue, int SessionCount);
public record DailyRevenueDto(DateTime Date, decimal Revenue);

public record AnalyticsSummaryDto(
    decimal TotalRevenue,
    decimal TodayRevenue,
    int ActiveSessions,
    int CompletedSessions,
    int TotalSlots,
    int OccupiedSlots,
    IEnumerable<RevenueByVehicleTypeDto> RevenueByType,
    IEnumerable<DailyRevenueDto> Last7DaysRevenue
);

// --- Reconciliation ---
public record StaffReconciliationDto(
    int StaffId,
    string StaffName,
    int SessionsHandled,
    decimal CashCollected,
    decimal UpiCollected,
    decimal CardCollected,
    decimal TotalCollected
);

// --- User Management ---
public record AdminUserDto(
    int Id,
    int? BranchId,
    string? BranchName,
    string FullName,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt
);

public record CreateAdminUserDto(
    int BranchId,
    string FullName,
    string Email,
    string Password
);

public record UpdateUserStatusDto(bool IsActive);
