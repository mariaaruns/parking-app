using ParkingApp.Domain.Enums;

namespace ParkingApp.Application.DTOs.SuperAdmin;

public record BranchDto(int Id, string Name, string Address, bool IsActive, DateTime CreatedAt);
public record CreateBranchDto(string Name, string Address);
public record UpdateBranchDto(string Name, string Address, bool IsActive);

public record CrossBranchSummaryDto(
    int BranchId,
    string BranchName,
    int TotalSlots,
    int OccupiedSlots,
    decimal OccupancyRate,
    decimal TodayRevenue,
    decimal TotalRevenue,
    int ActiveSessions
);
