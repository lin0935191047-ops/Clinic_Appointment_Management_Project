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

public class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
    {
        builder.ToTable("doctor_schedules", t =>
        {
            t.HasCheckConstraint("chk_doctor_schedules_day_of_week", "day_of_week >= 1 AND day_of_week <= 7");
            t.HasCheckConstraint("chk_doctor_schedules_time_order", "start_time < end_time");
            t.HasCheckConstraint("chk_doctor_schedules_effective_date_order", "effective_to IS NULL OR effective_to >= effective_from");
        });

        builder.HasKey(s => s.Id);

        builder.Property(s => s.DayOfWeek).IsRequired();
        builder.Property(s => s.StartTime).IsRequired();
        builder.Property(s => s.EndTime).IsRequired();
        builder.Property(s => s.EffectiveFrom).IsRequired();
        builder.Property(s => s.EffectiveTo).IsRequired(false);
        builder.Property(s => s.SlotDurationMinutes).IsRequired();
        builder.Property(s => s.IsActive).IsRequired();

        builder.HasOne(s => s.Doctor)
            .WithMany(d => d.Schedules)
            .HasForeignKey(s => s.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.DoctorId, s.DayOfWeek, s.IsActive });
    }
}

public class DoctorTimeOffConfiguration : IEntityTypeConfiguration<DoctorTimeOff>
{
    public void Configure(EntityTypeBuilder<DoctorTimeOff> builder)
    {
        builder.ToTable("doctor_time_offs", t =>
        {
            t.HasCheckConstraint("chk_doctor_time_offs_time_order", "start_at < end_at");
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.StartAt).IsRequired();
        builder.Property(t => t.EndAt).IsRequired();
        builder.Property(t => t.Reason).HasColumnType("text");
        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(t => t.Doctor)
            .WithMany(d => d.TimeOffs)
            .HasForeignKey(t => t.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.CreatedByUser)
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.DoctorId, t.Status, t.StartAt, t.EndAt });
    }
}

public class DoctorScheduleBlockConfiguration : IEntityTypeConfiguration<DoctorScheduleBlock>
{
    public void Configure(EntityTypeBuilder<DoctorScheduleBlock> builder)
    {
        builder.ToTable("doctor_schedule_blocks", t =>
        {
            t.HasCheckConstraint("chk_doctor_schedule_blocks_time_order", "start_at < end_at");
        });

        builder.HasKey(b => b.Id);

        builder.Property(b => b.StartAt).IsRequired();
        builder.Property(b => b.EndAt).IsRequired();
        builder.Property(b => b.Reason).HasColumnType("text");
        builder.Property(b => b.BlockType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(b => b.IsActive).IsRequired();

        builder.HasOne(b => b.Doctor)
            .WithMany(d => d.ScheduleBlocks)
            .HasForeignKey(b => b.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.CreatedByUser)
            .WithMany()
            .HasForeignKey(b => b.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => new { b.DoctorId, b.IsActive, b.StartAt, b.EndAt });
    }
}

