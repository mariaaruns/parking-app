using ParkingApp.Domain.Enums;

namespace ParkingApp.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public int ParkingSessionId { get; set; }
    public int EditedByUserId { get; set; }
    public AuditAction Action { get; set; }
    public string OldValuesJson { get; set; } = string.Empty;
    public string NewValuesJson { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation
    public ParkingSession ParkingSession { get; set; } = null!;
    public AppUser EditedByUser { get; set; } = null!;
}
