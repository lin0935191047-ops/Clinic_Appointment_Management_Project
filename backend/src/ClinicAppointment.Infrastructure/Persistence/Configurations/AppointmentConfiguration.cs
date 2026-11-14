using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicAppointment.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.BookingCode)
            .HasMaxLength(32)
            .IsRequired();
        builder.HasIndex(a => a.BookingCode).IsUnique();

        builder.Property(a => a.ServiceNameSnapshot)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(a => a.ServicePriceSnapshot)
            .HasPrecision(12, 2);

        builder.Property(a => a.PaidAmount)
            .HasPrecision(12, 2)
            .HasDefaultValue(0);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        // Unique Partial Index: Ngăn chặn 2 người đặt cùng 1 ca khám của bác sĩ tại một thời điểm
        builder.HasIndex(a => new { a.DoctorId, a.StartAt })
            .IsUnique()
            .HasFilter("status NOT IN ('Cancelled', 'NoShow')");

        builder.HasOne(a => a.Patient)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Doctor)
            .WithMany()
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Service)
            .WithMany()
            .HasForeignKey(a => a.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

