using ParkingApp.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ParkingApp.Domain.Entities;

public class ParkingSlot
{
    public int Id { get; set; }
    public int ZoneId { get; set; }
    public string SlotCode { get; set; } = string.Empty; // e.g., B-01, C-12, H-02
    public SlotStatus Status { get; set; } = SlotStatus.Available;

    [Timestamp]
    public byte[] RowVersion { get; set; } = []; // Concurrency token - prevents double booking

    // Navigation
    public ParkingZone Zone { get; set; } = null!;
    public ICollection<ParkingSession> Sessions { get; set; } = [];
}
