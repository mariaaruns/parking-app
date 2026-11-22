using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain.Entities;

namespace ParkingApp.Infrastructure.Configurations;

public class ParkingSessionConfiguration : IEntityTypeConfiguration<ParkingSession>
{
    public void Configure(EntityTypeBuilder<ParkingSession> builder)
    {
        builder.ToTable("ParkingSessions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.PublicToken).HasMaxLength(100).IsRequired();
        builder.HasIndex(s => s.PublicToken).IsUnique();

        builder.Property(s => s.VehicleNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(s => s.VehicleNumber);

        builder.Property(s => s.DriverPhone).HasMaxLength(15);
        builder.Property(s => s.CalculatedRent).HasColumnType("decimal(10,2)");
        builder.Property(s => s.EntryTime).IsRequired();
        builder.Property(s => s.DeletionReason).HasMaxLength(500);

        // Global soft-delete query filter
        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.HasOne(s => s.Branch)
               .WithMany(b => b.ParkingSessions)
               .HasForeignKey(s => s.BranchId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Slot)
               .WithMany(sl => sl.Sessions)
               .HasForeignKey(s => s.SlotId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.CheckedInByUser)
               .WithMany(u => u.CheckedInSessions)
               .HasForeignKey(s => s.CheckedInByUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.CheckedOutByUser)
               .WithMany(u => u.CheckedOutSessions)
               .HasForeignKey(s => s.CheckedOutByUserId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.DeletedByUser)
               .WithMany()
               .HasForeignKey(s => s.DeletedByUserId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
