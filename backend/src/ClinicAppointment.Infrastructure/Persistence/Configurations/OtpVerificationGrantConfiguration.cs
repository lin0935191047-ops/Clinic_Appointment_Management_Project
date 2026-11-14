using ClinicAppointment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicAppointment.Infrastructure.Persistence.Configurations;

public class OtpVerificationGrantConfiguration : IEntityTypeConfiguration<OtpVerificationGrant>
{
    public void Configure(EntityTypeBuilder<OtpVerificationGrant> builder)
    {
        builder.ToTable("otp_verification_grants");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ChallengeId)
            .IsRequired();

        builder.Property(x => x.AppointmentId)
            .IsRequired();

        builder.Property(x => x.Purpose)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.GrantTokenHash)
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.Property(x => x.ConsumedAt);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Unique index: Mỗi Challenge chỉ cấp duy nhất 1 Grant
        builder.HasIndex(x => x.ChallengeId)
            .IsUnique()
            .HasDatabaseName("ix_otp_verification_grants_challenge_id");

        // Unique index: GrantTokenHash là duy nhất
        builder.HasIndex(x => x.GrantTokenHash)
            .IsUnique()
            .HasDatabaseName("ix_otp_verification_grants_grant_token_hash");

        // Index hỗ trợ truy vấn theo AppointmentId và Purpose
        builder.HasIndex(x => new { x.Id, x.AppointmentId })
            .HasDatabaseName("ix_otp_verification_grants_id_appointment_id");

        builder.HasOne(x => x.Challenge)
            .WithMany()
            .HasForeignKey(x => x.ChallengeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Appointment)
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
