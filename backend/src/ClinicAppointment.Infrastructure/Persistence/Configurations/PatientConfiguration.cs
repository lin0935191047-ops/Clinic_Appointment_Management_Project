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

            // Nếu có cả 2 thì năm sinh phải khớp năm của DateOfBirth
            t.HasCheckConstraint("ck_patients_birth_year_matches_dob",
                "date_of_birth IS NULL OR birth_year IS NULL OR birth_year = EXTRACT(YEAR FROM date_of_birth)::smallint");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PatientCode)
            .HasMaxLength(32)
            .IsRequired();
        builder.HasIndex(p => p.PatientCode).IsUnique();

        builder.Property(p => p.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.NormalizedFullName)
            .HasMaxLength(200)
            .IsRequired();
        builder.HasIndex(p => p.NormalizedFullName);

        builder.Property(p => p.PhoneNumber)
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(p => p.PhoneNumber);

        builder.Property(p => p.Gender)
            .HasMaxLength(30);

        builder.Property(p => p.RecordStatus)
            .HasMaxLength(30)
            .IsRequired()
            .HasDefaultValue("ACTIVE");

        // UserId nullable, partial unique khi khác null (Patient độc lập User)
        builder.HasIndex(p => p.UserId)
            .IsUnique()
            .HasFilter("user_id IS NOT NULL");

        // Quan hệ với User (1 - 1 tùy chọn)
        builder.HasOne(p => p.User)
            .WithOne(u => u.PatientProfile)
            .HasForeignKey<Patient>(p => p.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Quan hệ tự tham chiếu khi gộp hồ sơ (Merge)
        builder.HasOne(p => p.MergedIntoPatient)
            .WithMany()
            .HasForeignKey(p => p.MergedIntoPatientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index cho exact search theo HMAC
        builder.HasIndex(p => p.NationalIdHmac)
            .HasFilter("national_id_hmac IS NOT NULL");
    }
}
