using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain.Entities;

namespace ParkingApp.Infrastructure.Configurations;

public class ParkingSlotConfiguration : IEntityTypeConfiguration<ParkingSlot>
{
    public void Configure(EntityTypeBuilder<ParkingSlot> builder)
    {
        builder.ToTable("ParkingSlots");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.SlotCode).HasMaxLength(20).IsRequired();
        builder.Property(s => s.Status).IsRequired();
        builder.Property(s => s.RowVersion).IsRowVersion(); // Concurrency token

        builder.HasOne(s => s.Zone)
               .WithMany(z => z.Slots)
               .HasForeignKey(s => s.ZoneId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.ZoneId, s.SlotCode }).IsUnique();
    }
}
