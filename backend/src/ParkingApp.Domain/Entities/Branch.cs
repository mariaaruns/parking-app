namespace ParkingApp.Domain.Entities;

public class Branch
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<AppUser> Users { get; set; } = [];
    public ICollection<ParkingZone> ParkingZones { get; set; } = [];
    public ICollection<RateCard> RateCards { get; set; } = [];
    public ICollection<ParkingSession> ParkingSessions { get; set; } = [];
}
