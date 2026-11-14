using ClinicAppointment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicAppointment.Infrastructure.Persistence.Configurations;

public class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    public void Configure(EntityTypeBuilder<Specialty> builder)
    {
        builder.ToTable("specialties");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(s => s.Code).IsUnique();
        builder.Property(s => s.Slug).HasMaxLength(120).IsRequired();
        builder.HasIndex(s => s.Slug).IsUnique();
        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
    }
}

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("services");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(s => s.Code).IsUnique();
        builder.Property(s => s.Name).HasMaxLength(150).IsRequired();
        builder.Property(s => s.Price).HasPrecision(12, 2);

        builder.HasOne(s => s.Specialty)
            .WithMany(sp => sp.Services)
            .HasForeignKey(s => s.SpecialtyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("doctors");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DoctorCode).HasMaxLength(32).IsRequired();
        builder.HasIndex(d => d.DoctorCode).IsUnique();
        builder.Property(d => d.FullName).HasMaxLength(150).IsRequired();

        builder.HasOne(d => d.Specialty)
            .WithMany()
            .HasForeignKey(d => d.SpecialtyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RescheduleRequestConfiguration : IEntityTypeConfiguration<RescheduleRequest>
{
    public void Configure(EntityTypeBuilder<RescheduleRequest> builder)
    {
        builder.ToTable("reschedule_requests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        // Chỉ cho phép tối đa 1 request ở trạng thái PendingReview cho mỗi appointment
        builder.HasIndex(r => r.AppointmentId)
            .IsUnique()
            .HasFilter("status = 'PendingReview'");
    }
}

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("refunds");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.PaidServiceAmount).HasPrecision(12, 2);
        builder.Property(r => r.CancellationFeeRate).HasPrecision(5, 2);
        builder.Property(r => r.CancellationFeeAmount).HasPrecision(12, 2);
        builder.Property(r => r.ExpectedRefundAmount).HasPrecision(12, 2);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
    }
}

