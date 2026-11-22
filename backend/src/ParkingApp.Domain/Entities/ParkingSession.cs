using ParkingApp.Domain.Enums;

namespace ParkingApp.Domain.Entities;

public class ParkingSession
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int SlotId { get; set; }
    public string PublicToken { get; set; } = string.Empty; // Unique UUID for QR access
    public string VehicleNumber { get; set; } = string.Empty; // Always uppercase, indexed
    public VehicleType VehicleType { get; set; }
    public string? DriverPhone { get; set; }

    public DateTime EntryTime { get; set; } = DateTime.UtcNow;
    public DateTime? ExitTime { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Active;

    public decimal CalculatedRent { get; set; }
    public PaymentMode? PaymentMode { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public int CheckedInByUserId { get; set; }
    public int? CheckedOutByUserId { get; set; }

    // Soft Delete
    public bool IsDeleted { get; set; } = false;
    public int? DeletedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletionReason { get; set; }

    // Navigation
    public Branch Branch { get; set; } = null!;
    public ParkingSlot Slot { get; set; } = null!;
    public AppUser CheckedInByUser { get; set; } = null!;
    public AppUser? CheckedOutByUser { get; set; }
    public AppUser? DeletedByUser { get; set; }
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
}
