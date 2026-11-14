using ClinicAppointment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicAppointment.Infrastructure.Persistence.Configurations;

public class ConsultationConfiguration : IEntityTypeConfiguration<Consultation>
{
    public void Configure(EntityTypeBuilder<Consultation> builder)
    {
        builder.ToTable("consultations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.AppointmentId)
            .IsRequired();

        builder.HasIndex(c => c.AppointmentId)
            .IsUnique();

        builder.Property(c => c.DoctorId)
            .IsRequired();

        builder.Property(c => c.Symptoms)
            .IsRequired();

        builder.Property(c => c.Diagnosis)
            .IsRequired();

        builder.Property(c => c.ExaminationNotes);
        builder.Property(c => c.Conclusion);
        builder.Property(c => c.FollowUpInstructions);
        builder.Property(c => c.ConsultedAt);

        builder.HasOne(c => c.Appointment)
            .WithOne(a => a.Consultation)
            .HasForeignKey<Consultation>(c => c.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Doctor)
            .WithMany()
            .HasForeignKey(c => c.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
