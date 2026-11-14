using ClinicAppointment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicAppointment.Infrastructure.Persistence.Configurations;

public class PatientIdentityReviewCaseConfiguration : IEntityTypeConfiguration<PatientIdentityReviewCase>
{
    public void Configure(EntityTypeBuilder<PatientIdentityReviewCase> builder)
    {
        builder.ToTable("patient_identity_review_cases", t =>
        {
            t.HasCheckConstraint("ck_identity_review_distinct_patients", 
                "patient_id <> candidate_patient_id");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.MatchReason)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.ReviewNote)
            .HasColumnType("text");

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // UNIQUE (patient_id, candidate_patient_id, match_reason)
        builder.HasIndex(c => new { c.PatientId, c.CandidatePatientId, c.MatchReason })
            .IsUnique();

        builder.HasOne(c => c.Patient)
            .WithMany()
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.CandidatePatient)
            .WithMany()
            .HasForeignKey(c => c.CandidatePatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ReviewedByUser)
            .WithMany()
            .HasForeignKey(c => c.ReviewedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class PatientMergeEventConfiguration : IEntityTypeConfiguration<PatientMergeEvent>
{
    public void Configure(EntityTypeBuilder<PatientMergeEvent> builder)
    {
        builder.ToTable("patient_merge_events", t =>
        {
            t.HasCheckConstraint("ck_patient_merge_distinct_patients", 
                "source_patient_id <> target_patient_id");
        });

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Reason)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(m => m.BeforeSnapshotJson)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(m => m.MergedAt)
            .IsRequired();

        builder.HasOne(m => m.SourcePatient)
            .WithMany()
            .HasForeignKey(m => m.SourcePatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.TargetPatient)
            .WithMany()
            .HasForeignKey(m => m.TargetPatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.MergedByUser)
            .WithMany()
            .HasForeignKey(m => m.MergedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
