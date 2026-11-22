using Microsoft.EntityFrameworkCore;
using ParkingApp.Domain.Entities;

namespace ParkingApp.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<ParkingZone> ParkingZones => Set<ParkingZone>();
    public DbSet<ParkingSlot> ParkingSlots => Set<ParkingSlot>();
    public DbSet<RateCard> RateCards => Set<RateCard>();
    public DbSet<ParkingSession> ParkingSessions => Set<ParkingSession>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Apply all IEntityTypeConfiguration from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    // Ignore soft-delete filter for specific queries
    public IQueryable<ParkingSession> AllSessions => ParkingSessions.IgnoreQueryFilters();
}
