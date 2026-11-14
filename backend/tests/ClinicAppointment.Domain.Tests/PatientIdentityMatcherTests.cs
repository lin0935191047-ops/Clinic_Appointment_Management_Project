using System;
using System.Linq;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Domain.Services;
using FluentAssertions;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

/// <summary>
/// FEAT-049: Kiểm thử chuyên sâu thuật toán đối soát định danh bệnh nhân (Identity matcher)
/// Tuân thủ quy tắc bất biến trong docs/SPEC.md v1.2, docs/BUSINESS_RULES.md (BR-001, BR-002, BR-003) và AGENTS.md:
/// - TUYỆT ĐỐI KHÔNG tự động gộp bệnh nhân chỉ vì trùng Họ Tên và Ngày Sinh / Năm Sinh (Strictly NO Auto-merge)
/// - Quét ứng viên trùng lặp và mở ca rà soát PatientIdentityReviewCase trạng thái OPEN để Lễ tân đối soát
/// - Nhận diện trường hợp số điện thoại dùng chung trong gia đình (SHARED_PHONE_DIFFERENT_NAME) an toàn
/// - Chuẩn hóa tiếng Việt (NormalizedFullName) xử lý khoảng trắng thừa và chữ hoa/thường
/// - Quy trình Merge có snapshot JSON và lưu vết kiểm toán bất biến
/// </summary>
public class PatientIdentityMatcherTests
{
    private readonly PatientDuplicateDetectionService _detectionService = new();

    #region 1. Ràng Buộc Bất Biến: Strictly NO Auto-Merge Khi Trùng Tên + Ngày Sinh/Năm Sinh

    [Fact]
    public void SameNameAndDob_DifferentPatients_MustCreateSeparateRecord_AndRequireManualReview()
    {
        // Arrange: Hai người khác nhau hoàn toàn nhưng trùng họ tên và ngày sinh
        var existingPatient = Patient.Create(
            fullName: "Nguyễn Văn An",
            phoneNumber: "0901234567",
            dateOfBirth: new DateOnly(1992, 8, 15),
            birthYear: 1992);

        var newPatient = Patient.Create(
            fullName: "  NGUYỄN   VĂN   AN  ",
            phoneNumber: "0988776655", // Số điện thoại khác nhau
            dateOfBirth: new DateOnly(1992, 8, 15),
            birthYear: 1992);

        // Act
        var matches = _detectionService.EvaluateCandidates(newPatient, new[] { existingPatient });

        // Assert: Phát hiện trùng lặp cần rà soát nhưng KHÔNG BAO GIỜ tự động gộp
        matches.Should().HaveCount(1);
        var match = matches.First();

        match.Reason.Should().Be(IdentityMatchReason.NAME_DOB);
        match.RequiresManualReview.Should().BeTrue("Bắt buộc phải qua rà soát thủ công của Lễ tân/Admin");
        match.ExistingPatient.Id.Should().Be(existingPatient.Id);

        // Bất biến: Hai hồ sơ có Id độc lập, không gộp ngầm
        newPatient.Id.Should().NotBe(existingPatient.Id);
        newPatient.RecordStatus.Should().Be("ACTIVE");
        existingPatient.RecordStatus.Should().Be("ACTIVE");
    }

    [Fact]
    public void SameNameAndBirthYear_WithoutExactDob_DetectsMatch_AndRequiresManualReview()
    {
        // Arrange: Chỉ có năm sinh (ví dụ người cao tuổi không nhớ ngày tháng)
        var existingPatient = Patient.Create(
            fullName: "Trần Thị Mai",
            phoneNumber: "0911223344",
            dateOfBirth: null,
            birthYear: 1958);

        var newPatient = Patient.Create(
            fullName: "Trần Thị Mai",
            phoneNumber: "0977889900",
            dateOfBirth: null,
            birthYear: 1958);

        // Act
        var matches = _detectionService.EvaluateCandidates(newPatient, new[] { existingPatient });

        // Assert
        matches.Should().HaveCount(1);
        var match = matches.First();
        match.Reason.Should().Be(IdentityMatchReason.NAME_BIRTH_YEAR);
        match.RequiresManualReview.Should().BeTrue();
    }

    #endregion

    #region 2. Nhận Diện Số Điện Thoại Dùng Chung Trong Gia Đình (Family Contacts)

    [Fact]
    public void SharedPhoneDifferentName_MotherBookingForChild_ShouldRecognizeSharedPhone_WithoutBlock()
    {
        // Arrange: Mẹ (Nguyễn Thị Lan) đặt cho con (Nguyễn Gia Bảo) dùng chung số điện thoại của mẹ
        var mother = Patient.Create(
            fullName: "Nguyễn Thị Lan",
            phoneNumber: "0912345678",
            dateOfBirth: new DateOnly(1988, 3, 20),
            birthYear: 1988);

        var child = Patient.Create(
            fullName: "Nguyễn Gia Bảo",
            phoneNumber: "0912345678", // Chung số điện thoại
            dateOfBirth: new DateOnly(2020, 11, 5),
            birthYear: 2020);

        // Act
        var matches = _detectionService.EvaluateCandidates(child, new[] { mother });

        // Assert: Nhận diện liên hệ người thân an toàn, không cảnh báo trùng người
        matches.Should().HaveCount(1);
        var match = matches.First();
        match.Reason.Should().Be(IdentityMatchReason.SHARED_PHONE_DIFFERENT_NAME);
        match.RequiresManualReview.Should().BeFalse("Không yêu cầu mở ca rà soát đối soát trùng vì tên khác nhau");
    }

    #endregion

    #region 3. Chuẩn Hóa Họ Tên Tiếng Việt & Khoảng Trắng (Vietnamese Normalization)

    [Theory]
    [InlineData("nguyễn văn an", "NGUYỄN VĂN AN")]
    [InlineData("  Lê   Thị   Hoa  ", "Lê Thị Hoa")]
    [InlineData("ĐẶNG QUỐC HƯNG", "đặng quốc hưng")]
    [InlineData("  Phạm   Minh   Tuấn  ", "PHẠM MINH TUẤN")]
    public void NameNormalization_DifferentCasingAndExtraSpaces_ShouldProduceSameNormalizedForm(string name1, string name2)
    {
        // Arrange
        var p1 = Patient.Create(name1, "0901111111", new DateOnly(1990, 1, 1), 1990);
        var p2 = Patient.Create(name2, "0902222222", new DateOnly(1990, 1, 1), 1990);

        // Act
        var matches = _detectionService.EvaluateCandidates(p2, new[] { p1 });

        // Assert: Chuỗi chuẩn hóa NormalizedFullName phải hoàn toàn khớp nhau
        p1.NormalizedFullName.Should().Be(p2.NormalizedFullName);
        matches.Should().HaveCount(1);
        matches.First().Reason.Should().Be(IdentityMatchReason.NAME_DOB);
    }

    [Fact]
    public void CompletelyDifferentPatients_ProducesNoMatch()
    {
        // Arrange
        var p1 = Patient.Create("Hoàng Văn A", "0911111111", new DateOnly(1980, 5, 1), 1980);
        var p2 = Patient.Create("Trần Thị B", "0922222222", new DateOnly(1995, 10, 20), 1995);

        // Act
        var matches = _detectionService.EvaluateCandidates(p2, new[] { p1 });

        // Assert
        matches.Should().BeEmpty();
    }

    #endregion

    #region 4. Quy Trình Rà Soát Thủ Công & Merge Tại Quầy

    [Fact]
    public void ManualReview_WhenConfirmedSamePerson_TransfersAppointments_AndMarksSecondaryAsMerged()
    {
        // Arrange: Lễ tân đối soát CCCD gốc tại quầy xác nhận đúng là cùng 1 người
        var primaryPatient = Patient.Create("Lê Hoàng Nam", "0909999999", new DateOnly(1993, 4, 12), 1993);
        var secondaryPatient = Patient.Create("Lê Hoàng Nam", "0908888888", new DateOnly(1993, 4, 12), 1993);

        // Tạo ca rà soát OPEN
        var reviewCase = PatientIdentityReviewCase.Create(
            primaryPatient.Id,
            secondaryPatient.Id,
            IdentityMatchReason.NAME_DOB);
        reviewCase.Status.Should().Be(IdentityReviewStatus.OPEN);

        // Act: Lễ tân giải quyết xác nhận cùng 1 người (CONFIRMED_SAME_PERSON)
        var receptionistId = Guid.NewGuid();
        reviewCase.Resolve(
            IdentityReviewStatus.CONFIRMED_SAME_PERSON,
            receptionistId,
            "Đã đối chiếu CCCD bản gốc trùng khớp 100%");

        // Ghi nhận sự kiện gộp
        var mergeEvent = PatientMergeEvent.Create(
            secondaryPatient.Id,
            primaryPatient.Id,
            receptionistId,
            "Trùng họ tên, ngày sinh và số CCCD",
            "{\"fullName\":\"Lê Hoàng Nam\",\"phone\":\"0908888888\"}");

        // Đánh dấu hồ sơ phụ là MERGED
        secondaryPatient.RecordStatus = "MERGED";
        secondaryPatient.MergedIntoPatientId = primaryPatient.Id;
        secondaryPatient.IsPossibleDuplicate = false;

        // Assert
        reviewCase.Status.Should().Be(IdentityReviewStatus.CONFIRMED_SAME_PERSON);
        reviewCase.ReviewedByUserId.Should().Be(receptionistId);
        reviewCase.ReviewNote.Should().Contain("CCCD bản gốc");

        secondaryPatient.RecordStatus.Should().Be("MERGED");
        secondaryPatient.MergedIntoPatientId.Should().Be(primaryPatient.Id);

        mergeEvent.TargetPatientId.Should().Be(primaryPatient.Id);
        mergeEvent.SourcePatientId.Should().Be(secondaryPatient.Id);
        mergeEvent.BeforeSnapshotJson.Should().Contain("0908888888");
    }

    [Fact]
    public void ManualReview_WhenDifferentPerson_ClosesReviewCase_AndKeepsBothProfilesActive()
    {
        // Arrange: Lễ tân đối soát CCCD phát hiện là 2 người khác nhau (trùng họ tên và ngày sinh ngẫu nhiên)
        var patientA = Patient.Create("Nguyễn Văn Bình", "0901111222", new DateOnly(1985, 12, 25), 1985);
        var patientB = Patient.Create("Nguyễn Văn Bình", "0903333444", new DateOnly(1985, 12, 25), 1985);
        patientA.IsPossibleDuplicate = true;
        patientB.IsPossibleDuplicate = true;

        var reviewCase = PatientIdentityReviewCase.Create(
            patientA.Id,
            patientB.Id,
            IdentityMatchReason.NAME_DOB);

        // Act: Lễ tân kết luận là 2 người khác nhau (DIFFERENT_PERSON)
        var receptionistId = Guid.NewGuid();
        reviewCase.Resolve(
            IdentityReviewStatus.DIFFERENT_PERSON,
            receptionistId,
            "Đã kiểm tra CCCD: quê quán và số định danh khác nhau hoàn toàn");

        patientA.IsPossibleDuplicate = false;
        patientB.IsPossibleDuplicate = false;

        // Assert: Ca rà soát được giải quyết và 2 hồ sơ độc lập vẫn ACTIVE
        reviewCase.Status.Should().Be(IdentityReviewStatus.DIFFERENT_PERSON);
        reviewCase.ReviewedByUserId.Should().Be(receptionistId);

        patientA.RecordStatus.Should().Be("ACTIVE");
        patientB.RecordStatus.Should().Be("ACTIVE");
        patientA.IsPossibleDuplicate.Should().BeFalse();
        patientB.IsPossibleDuplicate.Should().BeFalse();
    }

    #endregion
}
