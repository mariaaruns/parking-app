using ParkingApp.Domain.Enums;

namespace ParkingApp.Domain.Entities;

public class RateCard
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public VehicleType VehicleType { get; set; }
    public int GracePeriodMinutes { get; set; }   // Free grace window
    public decimal BaseHours { get; set; }         // Hours covered by BaseRate
    public decimal BaseRate { get; set; }          // Flat charge for BaseHours
    public decimal AdditionalHourlyRate { get; set; } // Per hour beyond BaseHours
    public decimal DailyMaxCap { get; set; }       // Max charge per 24-hour cycle

    // Navigation
    public Branch Branch { get; set; } = null!;
}
