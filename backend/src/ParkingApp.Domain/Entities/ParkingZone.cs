using ParkingApp.Domain.Enums;

namespace ParkingApp.Domain.Entities;

public class ParkingZone
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., Zone-B, Zone-C, Zone-H
    public VehicleType VehicleType { get; set; }

    // Navigation
    public Branch Branch { get; set; } = null!;
    public ICollection<ParkingSlot> Slots { get; set; } = [];
}
