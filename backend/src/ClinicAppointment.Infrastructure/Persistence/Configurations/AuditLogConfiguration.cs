using ClinicAppointment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicAppointment.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ActorType)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(a => a.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.EntityType)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(a => a.CorrelationId)
            .HasMaxLength(80);

        builder.Property(a => a.MetadataRedacted)
            .HasColumnType("text");

        builder.Property(a => a.OccurredAt)
            .IsRequired();

        builder.HasIndex(a => new { a.EntityType, a.EntityId, a.OccurredAt })
            .HasDatabaseName("ix_audit_logs_entity_type_entity_id_occurred_at");

        builder.HasIndex(a => new { a.ActorUserId, a.OccurredAt })
            .HasDatabaseName("ix_audit_logs_actor_user_id_occurred_at");

        builder.HasOne(a => a.ActorUser)
            .WithMany()
            .HasForeignKey(a => a.ActorUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
