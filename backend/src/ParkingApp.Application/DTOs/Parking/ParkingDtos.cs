using ParkingApp.Domain.Enums;

namespace ParkingApp.Application.DTOs.Parking;

// --- Slot Status ---
public record SlotStatusDto(
    int SlotId,
    string SlotCode,
    SlotStatus Status
);

public record ZoneStatusDto(
    int ZoneId,
    string ZoneName,
    VehicleType VehicleType,
    int TotalSlots,
    int AvailableSlots,
    IEnumerable<SlotStatusDto> Slots
);

public record BranchSlotStatusDto(
    int BranchId,
    string BranchName,
    IEnumerable<ZoneStatusDto> Zones
);

// --- Check-In ---
public record CheckInRequestDto(
    int? BranchId,
    int SlotId,
    string VehicleNumber,
    VehicleType VehicleType,
    string? DriverPhone
);

public record CheckInResponseDto(
    int SessionId,
    string PublicToken,
    string TicketUrl,
    string SlotCode,
    string ZoneName,
    string VehicleNumber,
    VehicleType VehicleType,
    string? DriverPhone,
    DateTime EntryTime
);

// --- Session Detail ---
public record ParkingSessionDto(
    int Id,
    string PublicToken,
    int BranchId,
    string BranchName,
    int SlotId,
    string SlotCode,
    int ZoneId,
    string ZoneName,
    string VehicleNumber,
    VehicleType VehicleType,
    string? DriverPhone,
    DateTime EntryTime,
    DateTime? ExitTime,
    SessionStatus Status,
    decimal CalculatedRent,
    PaymentMode? PaymentMode,
    PaymentStatus PaymentStatus,
    string CheckedInBy,
    string? CheckedOutBy,
    bool IsDeleted,
    string? DeletionReason,
    DateTime? DeletedAt
);

// --- Edit Session ---
public record EditSessionRequestDto(
    int? SlotId,
    string? VehicleNumber,
    VehicleType? VehicleType,
    string? DriverPhone,
    string? Reason
);

// --- Checkout ---
public record CheckoutRequestDto(
    PaymentMode PaymentMode
);

public record CheckoutResponseDto(
    int SessionId,
    string PublicToken,
    string SlotCode,
    string ZoneName,
    string VehicleNumber,
    DateTime EntryTime,
    DateTime ExitTime,
    string Duration,
    decimal FinalRent,
    PaymentMode PaymentMode
);

// --- Soft Delete ---
public record SoftDeleteRequestDto(string Reason);

// --- Rent Preview ---
public record RentPreviewDto(
    decimal CurrentRent,
    string ElapsedDuration,
    bool IsGracePeriod
);

// --- Capacity Strip ---
public record CapacityStripDto(
    int TotalBike, int AvailableBike,
    int TotalCar, int AvailableCar,
    int TotalHeavy, int AvailableHeavy
);
