using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain.Entities;

namespace ParkingApp.Infrastructure.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.OldValuesJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(a => a.NewValuesJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(a => a.Reason).HasMaxLength(500).IsRequired();
        builder.Property(a => a.Timestamp).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(a => a.ParkingSession)
               .WithMany(s => s.AuditLogs)
               .HasForeignKey(a => a.ParkingSessionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.EditedByUser)
               .WithMany(u => u.AuditLogs)
               .HasForeignKey(a => a.EditedByUserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
