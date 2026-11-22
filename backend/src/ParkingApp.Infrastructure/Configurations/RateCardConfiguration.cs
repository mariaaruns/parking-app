using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain.Entities;

namespace ParkingApp.Infrastructure.Configurations;

public class RateCardConfiguration : IEntityTypeConfiguration<RateCard>
{
    public void Configure(EntityTypeBuilder<RateCard> builder)
    {
        builder.ToTable("RateCards");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.VehicleType).IsRequired();
        builder.Property(r => r.BaseRate).HasColumnType("decimal(10,2)");
        builder.Property(r => r.AdditionalHourlyRate).HasColumnType("decimal(10,2)");
        builder.Property(r => r.DailyMaxCap).HasColumnType("decimal(10,2)");
        builder.Property(r => r.BaseHours).HasColumnType("decimal(5,2)");

        builder.HasOne(r => r.Branch)
               .WithMany(b => b.RateCards)
               .HasForeignKey(r => r.BranchId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.BranchId, r.VehicleType }).IsUnique();
    }
}
