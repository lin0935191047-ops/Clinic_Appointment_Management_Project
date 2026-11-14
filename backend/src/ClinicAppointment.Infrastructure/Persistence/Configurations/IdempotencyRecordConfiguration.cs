using ClinicAppointment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicAppointment.Infrastructure.Persistence.Configurations;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Scope)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.KeyHash)
            .IsRequired();

        builder.Property(x => x.RequestHash)
            .IsRequired();

        builder.Property(x => x.ResourceType)
            .HasMaxLength(50);

        builder.Property(x => x.ResponseBodyJson)
            .HasColumnType("text");

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Ràng buộc Unique (Scope, KeyHash) chống trùng lặp theo Database Design
        builder.HasIndex(x => new { x.Scope, x.KeyHash })
            .IsUnique()
            .HasDatabaseName("ix_idempotency_records_scope_key_hash");
    }
}
