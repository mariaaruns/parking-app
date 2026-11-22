using ParkingApp.Domain.Enums;

namespace ParkingApp.Application.DTOs.Public;

public record PublicTicketDto(
    string PublicToken,
    string SlotCode,
    string ZoneName,
    string VehicleNumber,
    VehicleType VehicleType,
    string? DriverPhone,
    DateTime EntryTime,
    DateTime? ExitTime,
    SessionStatus Status,
    string ElapsedDuration,
    decimal CurrentRent,
    PaymentMode? PaymentMode,
    PaymentStatus PaymentStatus,
    string BranchName,
    string BranchAddress
);
