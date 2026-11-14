using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Domain.Services;
using FluentAssertions;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class PatientDuplicateReviewTests
{
    private readonly PatientDuplicateDetectionService _detectionService = new();

    #region 1. Name + DOB/BirthYear Match (Strictly NO Auto-merge)

    [Fact]
    public void CandidateMatching_SameNameAndDob_ShouldDetectNameDobMatch_AndRequireManualReview()
    {
        var existing = Patient.Create("Nguyen Van An", "0912345678", new DateOnly(1990, 5, 10));
        var newPatient = Patient.Create("  NGUYEN   VAN   AN  ", "0988888888", new DateOnly(1990, 5, 10));

        var matches = _detectionService.EvaluateCandidates(newPatient, new[] { existing });

        matches.Should().HaveCount(1);
        var match = matches.First();
        match.Reason.Should().Be(IdentityMatchReason.NAME_DOB);
        match.RequiresManualReview.Should().BeTrue();
        match.ExistingPatient.Id.Should().Be(existing.Id);

        // Đảm bảo không tự động gộp (2 Id hoàn toàn khác nhau)
        newPatient.Id.Should().NotBe(existing.Id);
        newPatient.RecordStatus.Should().Be("ACTIVE");
    }

    [Fact]
    public void CandidateMatching_SameNameAndBirthYear_ShouldDetectNameBirthYearMatch()
    {
        var existing = Patient.Create("Tran Thi Mai", "0912345678", null, 1985);
        var newPatient = Patient.Create("Tran Thi Mai", "0977777777", null, 1985);

        var matches = _detectionService.EvaluateCandidates(newPatient, new[] { existing });

        matches.Should().HaveCount(1);
        matches.First().Reason.Should().Be(IdentityMatchReason.NAME_BIRTH_YEAR);
        matches.First().RequiresManualReview.Should().BeTrue();
    }

    #endregion

    #region 2. Shared Phone Contacts (Safe Family/Guardian Contacts)

    [Fact]
    public void CandidateMatching_SamePhoneDifferentName_ShouldBeRecognizedAsSharedPhone_WithoutManualReview()
    {
        // Ví dụ: Mẹ đặt cho con nhỏ, hoặc con đặt cho cha mẹ
        var parent = Patient.Create("Nguyen Van Cha", "0912345678", new DateOnly(1970, 1, 1));
        var child = Patient.Create("Nguyen Van Con", "0912345678", new DateOnly(2015, 6, 1));

        var matches = _detectionService.EvaluateCandidates(child, new[] { parent });

        matches.Should().HaveCount(1);
        var match = matches.First();
        match.Reason.Should().Be(IdentityMatchReason.SHARED_PHONE_DIFFERENT_NAME);
        match.RequiresManualReview.Should().BeFalse(); // An toàn, không kích hoạt cờ nghi trùng
    }

    #endregion

    #region 3. Review Case & Merge Audit Lifecycle

    [Fact]
    public void ReviewCase_Create_WithSamePatientId_ShouldThrowArgumentException()
    {
        var patientId = Guid.NewGuid();

        var act = () => PatientIdentityReviewCase.Create(patientId, patientId, IdentityMatchReason.NAME_DOB);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*không được trùng nhau*");
    }

    [Fact]
    public void ReviewCase_ResolveAsDifferentPerson_ShouldUpdateStatusAndAuditNote()
    {
        var patientA = Patient.Create("Le Van A", "0901111111", new DateOnly(1992, 1, 1));
        var patientB = Patient.Create("Le Van A", "0902222222", new DateOnly(1992, 1, 1));
        var reviewerUserId = Guid.NewGuid();

        var reviewCase = PatientIdentityReviewCase.Create(patientA.Id, patientB.Id, IdentityMatchReason.NAME_DOB);

        reviewCase.Resolve(IdentityReviewStatus.DIFFERENT_PERSON, reviewerUserId, "Đã kiểm tra CCCD tại quầy: hai người khác quê quán.");

        reviewCase.Status.Should().Be(IdentityReviewStatus.DIFFERENT_PERSON);
        reviewCase.ReviewedByUserId.Should().Be(reviewerUserId);
        reviewCase.ReviewedAt.Should().NotBeNull();
        reviewCase.ReviewNote.Should().Contain("hai người khác quê quán");
    }

    [Fact]
    public void PatientMergeEvent_Create_WithAudit_ShouldRecordBeforeSnapshot()
    {
        var sourcePatientId = Guid.NewGuid();
        var targetPatientId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var snapshot = "{\"PatientCode\":\"PAT-01\",\"FullName\":\"Tran B\",\"AppointmentsCount\":3}";

        var mergeEvent = PatientMergeEvent.Create(
            sourcePatientId,
            targetPatientId,
            reviewerId,
            "Bệnh nhân xác nhận trùng hồ sơ cũ",
            snapshot
        );

        mergeEvent.SourcePatientId.Should().Be(sourcePatientId);
        mergeEvent.TargetPatientId.Should().Be(targetPatientId);
        mergeEvent.MergedByUserId.Should().Be(reviewerId);
        mergeEvent.Reason.Should().Be("Bệnh nhân xác nhận trùng hồ sơ cũ");
        mergeEvent.BeforeSnapshotJson.Should().Be(snapshot);
        mergeEvent.MergedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void PatientMergeEvent_Create_WithSameSourceAndTarget_ShouldThrowArgumentException()
    {
        var sameId = Guid.NewGuid();

        var act = () => PatientMergeEvent.Create(sameId, sameId, Guid.NewGuid(), "Lý do", "{}");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Hồ sơ nguồn và hồ sơ đích không được trùng nhau*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void PatientMergeEvent_Create_WithEmptyReason_ShouldThrowArgumentException(string? invalidReason)
    {
        var act = () => PatientMergeEvent.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), invalidReason!, "{}");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Lý do gộp hồ sơ bắt buộc phải được ghi rõ*");
    }

    #endregion
}
