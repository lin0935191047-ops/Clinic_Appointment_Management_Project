using ClinicAppointment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicAppointment.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id");

        builder.Property(n => n.PatientId)
            .HasColumnName("patient_id");

        builder.Property(n => n.AppointmentId)
            .HasColumnName("appointment_id");

        builder.Property(n => n.Channel)
            .HasColumnName("channel")
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(n => n.TemplateCode)
            .HasColumnName("template_code")
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(n => n.RecipientCiphertext)
            .HasColumnName("recipient_ciphertext");

        builder.Property(n => n.RecipientMasked)
            .HasColumnName("recipient_masked")
            .HasMaxLength(100);

        builder.Property(n => n.PayloadJson)
            .HasColumnName("payload_json")
            .HasColumnType("jsonb");

        builder.Property(n => n.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(n => n.ProviderMessageId)
            .HasColumnName("provider_message_id")
            .HasMaxLength(150);

        builder.Property(n => n.SentAt)
            .HasColumnName("sent_at");

        builder.Property(n => n.FailureReason)
            .HasColumnName("failure_reason");

        builder.Property(n => n.RetryCount)
            .HasColumnName("retry_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(n => n.MaxRetries)
            .HasColumnName("max_retries")
            .HasDefaultValue(3)
            .IsRequired();

        builder.Property(n => n.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(n => n.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(n => new { n.Status, n.CreatedAt })
            .HasDatabaseName("ix_notifications_status_created_at");

        builder.HasIndex(n => n.AppointmentId)
            .HasDatabaseName("ix_notifications_appointment_id");

        builder.HasIndex(n => n.PatientId)
            .HasDatabaseName("ix_notifications_patient_id");

        // Relationships
        builder.HasOne(n => n.Patient)
            .WithMany()
            .HasForeignKey(n => n.PatientId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(n => n.Appointment)
            .WithMany()
            .HasForeignKey(n => n.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
