using ParkingApp.Domain.Enums;

namespace ParkingApp.Domain.Entities;

public class AppUser
{
    public int Id { get; set; }
    public int? BranchId { get; set; } // Nullable for SuperAdmin
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Branch? Branch { get; set; }
    public ICollection<ParkingSession> CheckedInSessions { get; set; } = [];
    public ICollection<ParkingSession> CheckedOutSessions { get; set; } = [];
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
}
