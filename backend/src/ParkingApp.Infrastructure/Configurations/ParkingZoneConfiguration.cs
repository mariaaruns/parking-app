using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain.Entities;

namespace ParkingApp.Infrastructure.Configurations;

public class ParkingZoneConfiguration : IEntityTypeConfiguration<ParkingZone>
{
    public void Configure(EntityTypeBuilder<ParkingZone> builder)
    {
        builder.ToTable("ParkingZones");
        builder.HasKey(z => z.Id);
        builder.Property(z => z.Name).HasMaxLength(50).IsRequired();
        builder.Property(z => z.VehicleType).IsRequired();

        builder.HasOne(z => z.Branch)
               .WithMany(b => b.ParkingZones)
               .HasForeignKey(z => z.BranchId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
