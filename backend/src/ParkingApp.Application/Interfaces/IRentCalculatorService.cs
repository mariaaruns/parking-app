using ParkingApp.Domain.Entities;
using ParkingApp.Domain.Enums;

namespace ParkingApp.Application.Interfaces;

public interface IRentCalculatorService
{
    /// <summary>
    /// Calculate current/final rent for a session based on the branch's RateCard.
    /// </summary>
    Task<decimal> CalculateRentAsync(int branchId, VehicleType vehicleType, DateTime entryTime, DateTime? exitTime = null);

    /// <summary>
    /// Get a formatted elapsed duration string "Xh Ym".
    /// </summary>
    string FormatDuration(DateTime entryTime, DateTime? exitTime = null);

    /// <summary>
    /// Check if still within grace period.
    /// </summary>
    Task<bool> IsGracePeriodAsync(int branchId, VehicleType vehicleType, DateTime entryTime);
}
