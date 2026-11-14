using ClinicAppointment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicAppointment.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients", t =>
        {
            // Ràng buộc bắt buộc có DateOfBirth hoặc BirthYear
            t.HasCheckConstraint("ck_patients_dob_or_birth_year", 
                "date_of_birth IS NOT NULL OR birth_year IS NOT NULL");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PatientCode)
            .HasMaxLength(32)
            .IsRequired();
        builder.HasIndex(p => p.PatientCode).IsUnique();

        builder.Property(p => p.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.PhoneNumber)
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(p => p.PhoneNumber);

        // UserId nullable, partial unique khi khác null
        builder.HasIndex(p => p.UserId)
            .IsUnique()
            .HasFilter("user_id IS NOT NULL");

        // Index cho exact search theo HMAC
        builder.HasIndex(p => p.NationalIdHmac)
            .HasFilter("national_id_hmac IS NOT NULL");
    }
}

