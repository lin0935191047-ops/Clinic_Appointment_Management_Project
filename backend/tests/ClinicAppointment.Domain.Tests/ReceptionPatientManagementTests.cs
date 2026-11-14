using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Patients.Commands;
using ClinicAppointment.Application.Features.Patients.DTOs;
using ClinicAppointment.Application.Features.Patients.Queries;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Domain.Services;
using ClinicAppointment.Infrastructure.Persistence;
using ClinicAppointment.Infrastructure.Security;
using ClinicAppointment.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class ReceptionPatientManagementTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private (IPatientSecurityService SecurityService, IIdentifierMaskingService MaskingService, IPatientDuplicateDetectionService DuplicateService) CreateServices(ApplicationDbContext db)
    {
        var encKey = SHA256.HashData(Encoding.UTF8.GetBytes("Test_Encryption_Secret_Key_32B!"));
        var hmacKey = SHA256.HashData(Encoding.UTF8.GetBytes("Test_HMAC_Secret_Key_Number_1!"));
        var piiService = new PiiEncryptionService(encKey);
        var hmacService = new HmacService(hmacKey);
        var maskingService = new IdentifierMaskingService();
        var securityService = new PatientSecurityService(piiService, hmacService, maskingService, db, NullLogger<PatientSecurityService>.Instance);
        var duplicateService = new PatientDuplicateDetectionService();
        return (securityService, maskingService, duplicateService);
    }

    [Fact]
    public async Task RoleGuard_StaffOnly_EnforcedForReceptionEndpoints()
    {
        // Arrange: Người dùng là Bệnh nhân (hoặc chưa đăng nhập) cố gắng tìm kiếm hoặc cập nhật hồ sơ tại quầy
        using var db = CreateInMemoryDbContext();
        var (security, masking, _) = CreateServices(db);

        var patientUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.PatientCode },
            IsAuthenticated = true
        };

        var searchHandler = new SearchReceptionPatientsQueryHandler(db, patientUser, security, masking);
        var updateHandler = new UpdatePatientCommandHandler(db, patientUser, security, masking);

        // Act 1: Tìm kiếm hồ sơ tại quầy
        var searchResult = await searchHandler.Handle(new SearchReceptionPatientsQuery(), CancellationToken.None);

        // Act 2: Cập nhật hồ sơ bệnh nhân
        var updateResult = await updateHandler.Handle(new UpdatePatientCommand(
            PatientId: Guid.NewGuid(),
            FullName: "Nguyễn Văn Thử",
            PhoneNumber: "0911223344",
            DateOfBirth: new DateOnly(1990, 1, 1)
        ), CancellationToken.None);

        // Assert: Cả 2 đều bị từ chối với HTTP 403 Forbidden
        searchResult.IsSuccess.Should().BeFalse();
        searchResult.StatusCode.Should().Be(403);
        searchResult.Error.Should().Contain("Bạn không có quyền truy cập");

        updateResult.IsSuccess.Should().BeFalse();
        updateResult.StatusCode.Should().Be(403);
        updateResult.Error.Should().Contain("Bạn không có quyền chỉnh sửa");
    }

    [Fact]
    public async Task RoleGuard_ReceptionistAndAdmin_AllowedAccess()
    {
        // Arrange: Người dùng có vai trò Receptionist
        using var db = CreateInMemoryDbContext();
        var (security, masking, _) = CreateServices(db);

        var p = Patient.Create("Đỗ Nhật Nam", "0909123456", new DateOnly(1998, 5, 12), 1998);
        db.Patients.Add(p);
        await db.SaveChangesAsync();

        var receptionistUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Email = "receptionist.main@clinic.vn",
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var searchHandler = new SearchReceptionPatientsQueryHandler(db, receptionistUser, security, masking);

        // Act
        var result = await searchHandler.Handle(new SearchReceptionPatientsQuery(Keyword: "Nhật Nam"), CancellationToken.None);

        // Assert: Thành công 200 OK
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle();
        result.Value.Items.First().FullName.Should().Be("Đỗ Nhật Nam");
    }

    [Fact]
    public async Task SearchPatients_ByKeyword_ReturnsMatchingPatients()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (security, masking, _) = CreateServices(db);

        var p1 = Patient.Create("Phạm Thanh Hằng", "0933111222", new DateOnly(1993, 4, 15), 1993);
        var p2 = Patient.Create("Lê Thanh Tùng", "0944222333", new DateOnly(1987, 8, 20), 1987);
        var p3 = Patient.Create("Phạm Quốc Anh", "0955333444", new DateOnly(1995, 12, 1), 1995);

        db.Patients.AddRange(p1, p2, p3);
        await db.SaveChangesAsync();

        var staff = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new SearchReceptionPatientsQueryHandler(db, staff, security, masking);

        // Act: Tìm kiếm từ khóa "Thanh"
        var result = await handler.Handle(new SearchReceptionPatientsQuery(Keyword: "Thanh"), CancellationToken.None);

        // Assert: Trả về 2 bệnh nhân có tên chứa "Thanh"
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Items.Select(p => p.FullName).Should().Contain(new[] { "Phạm Thanh Hằng", "Lê Thanh Tùng" });
    }

    [Fact]
    public async Task SearchPatients_FilterByDuplicateFlagAndStatus()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (security, masking, _) = CreateServices(db);

        var p1 = Patient.Create("Bệnh Nhân A", "0911000111", new DateOnly(1990, 1, 1), 1990);
        p1.IsPossibleDuplicate = true;

        var p2 = Patient.Create("Bệnh Nhân B", "0922000222", new DateOnly(1992, 2, 2), 1992);
        p2.IsPossibleDuplicate = false;

        db.Patients.AddRange(p1, p2);
        await db.SaveChangesAsync();

        var staff = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.AdminCode },
            IsAuthenticated = true
        };

        var handler = new SearchReceptionPatientsQueryHandler(db, staff, security, masking);

        // Act: Lọc các hồ sơ nghi trùng
        var result = await handler.Handle(new SearchReceptionPatientsQuery(IsPossibleDuplicate: true), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle();
        result.Value.Items.First().FullName.Should().Be("Bệnh Nhân A");
    }

    [Fact]
    public async Task UpdatePatient_ValidData_UpdatesInformationSuccessfully()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (security, masking, _) = CreateServices(db);

        var patient = Patient.Create("Trần Văn Cũ", "0909000111", new DateOnly(1991, 1, 1), 1991);
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        var staff = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new UpdatePatientCommandHandler(db, staff, security, masking);

        // Act: Cập nhật họ tên mới, số điện thoại mới, CCCD mới
        var result = await handler.Handle(new UpdatePatientCommand(
            PatientId: patient.Id,
            FullName: "Trần Văn Mới",
            PhoneNumber: "0988777666",
            DateOfBirth: new DateOnly(1991, 5, 20),
            BirthYear: 1991,
            Email: "tranmoi@example.com",
            Gender: "NAM",
            Address: "123 Hai Bà Trưng, Hà Nội",
            NationalId: "001091009988"
        ), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.FullName.Should().Be("Trần Văn Mới");
        result.Value.PhoneNumber.Should().Be("0988777666");
        result.Value.Address.Should().Be("123 Hai Bà Trưng, Hà Nội");
        result.Value.MaskedNationalId.Should().NotBeNull();
        result.Value.MaskedNationalId.Should().Contain("***");

        // Kiểm tra audit log
        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "UPDATE_PATIENT" && a.EntityId == patient.Id);
        audit.Should().NotBeNull();
        audit!.ActorType.Should().Be("STAFF");
        audit.ActorUserId.Should().Be(staff.UserId);
    }

    [Fact]
    public async Task NoAutoMerge_WhenDuplicateNameAndDOB_CreatesIndependentProfileAndFlagsReviewCase()
    {
        // Arrange: Đã có hồ sơ bệnh nhân Nguyễn Văn A sinh ngày 10/10/1990 trong hệ thống
        using var db = CreateInMemoryDbContext();
        var (security, masking, duplicate) = CreateServices(db);

        var existingPatient = Patient.Create("Nguyễn Văn A", "0901111222", new DateOnly(1990, 10, 10), 1990);
        db.Patients.Add(existingPatient);
        await db.SaveChangesAsync();

        var initialCount = await db.Patients.CountAsync();
        var existingPatientCode = existingPatient.PatientCode;

        // Act: Lễ tân tạo một hồ sơ mới cũng có tên Nguyễn Văn A và ngày sinh 10/10/1990 nhưng số điện thoại khác
        var createHandler = new CreatePatientCommandHandler(db, security, duplicate, masking, NullLogger<CreatePatientCommandHandler>.Instance);
        var createResult = await createHandler.Handle(new CreatePatientCommand(
            FullName: "Nguyễn Văn A",
            PhoneNumber: "0988999888",
            DateOfBirth: new DateOnly(1990, 10, 10),
            BirthYear: 1990
        ), CancellationToken.None);

        // Assert: TUYỆT ĐỐI KHÔNG AUTO-MERGE
        createResult.IsSuccess.Should().BeTrue();
        createResult.Value!.Id.Should().NotBe(existingPatient.Id, "Hồ sơ mới phải có Id độc lập, không được gộp vào hồ sơ cũ");
        createResult.Value.PatientCode.Should().NotBe(existingPatientCode, "Hồ sơ mới phải có mã hồ sơ riêng biệt");

        var newCount = await db.Patients.CountAsync();
        newCount.Should().Be(initialCount + 1, "Số lượng hồ sơ phải tăng thêm 1 (hai hồ sơ tồn tại song song)");

        // Kiểm tra ca rà soát được tạo ở trạng thái OPEN
        var reviewCase = await db.PatientIdentityReviewCases
            .FirstOrDefaultAsync(c => (c.PatientId == existingPatient.Id && c.CandidatePatientId == createResult.Value.Id) ||
                                      (c.CandidatePatientId == existingPatient.Id && c.PatientId == createResult.Value.Id));

        reviewCase.Should().NotBeNull("Hệ thống phải mở ca rà soát trùng lặp cho Lễ tân");
        reviewCase!.Status.Should().Be(IdentityReviewStatus.OPEN);
        reviewCase.MatchReason.Should().Be(IdentityMatchReason.NAME_DOB);
    }

    [Fact]
    public async Task NoAutoMerge_WhenUpdatingToMatchingNameAndDOB_FlagsDuplicateAndOpensReviewCase()
    {
        // Arrange: Có hồ sơ P1 ("Võ Hoàng Yến", 1992-06-15) và hồ sơ P2 ("Võ Yến", 1995-01-01)
        using var db = CreateInMemoryDbContext();
        var (security, masking, _) = CreateServices(db);

        var p1 = Patient.Create("Võ Hoàng Yến", "0901000111", new DateOnly(1992, 6, 15), 1992);
        var p2 = Patient.Create("Võ Yến", "0902000222", new DateOnly(1995, 1, 1), 1995);
        db.Patients.AddRange(p1, p2);
        await db.SaveChangesAsync();

        var staff = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new UpdatePatientCommandHandler(db, staff, security, masking);

        // Act: Cập nhật hồ sơ P2 đổi tên và ngày sinh thành trùng khớp với P1 ("Võ Hoàng Yến", 1992-06-15)
        var result = await handler.Handle(new UpdatePatientCommand(
            PatientId: p2.Id,
            FullName: "Võ Hoàng Yến",
            PhoneNumber: p2.PhoneNumber,
            DateOfBirth: new DateOnly(1992, 6, 15),
            BirthYear: 1992
        ), CancellationToken.None);

        // Assert: Không auto-merge, P2 vẫn tồn tại độc lập và được gắn cờ IsPossibleDuplicate = true
        result.IsSuccess.Should().BeTrue();
        var updatedP2 = await db.Patients.FindAsync(p2.Id);
        updatedP2!.IsPossibleDuplicate.Should().BeTrue();
        updatedP2.RecordStatus.Should().Be("ACTIVE");

        // Kiểm tra ca rà soát OPEN được tạo giữa p1 và p2
        var reviewCase = await db.PatientIdentityReviewCases
            .FirstOrDefaultAsync(c => (c.PatientId == p1.Id && c.CandidatePatientId == p2.Id) ||
                                      (c.CandidatePatientId == p1.Id && c.PatientId == p2.Id));

        reviewCase.Should().NotBeNull();
        reviewCase!.Status.Should().Be(IdentityReviewStatus.OPEN);
        reviewCase.MatchReason.Should().Be(IdentityMatchReason.NAME_DOB);
    }

    [Fact]
    public async Task MergeCaseAudited_WhenConfirmedSamePerson_TransfersAppointmentsAndCreatesAuditLog()
    {
        // Arrange: Có 2 hồ sơ nghi trùng, hồ sơ P1 có 1 ca khám, hồ sơ P2 có 2 ca khám
        using var db = CreateInMemoryDbContext();
        var (security, masking, _) = CreateServices(db);

        var p1 = Patient.Create("Bùi Anh Tuấn", "0911222333", new DateOnly(1991, 9, 9), 1991);
        var p2 = Patient.Create("Bùi Anh Tuấn", "0922333444", new DateOnly(1991, 9, 9), 1991);
        p1.IsPossibleDuplicate = true;
        p2.IsPossibleDuplicate = true;
        db.Patients.AddRange(p1, p2);

        var doctorUser = User.Create("doc.merge@clinic.vn", "hash");
        var spec = Specialty.Create("CK-RHM", "Răng Hàm Mặt", "rang-ham-mat");
        db.Users.Add(doctorUser);
        db.Specialties.Add(spec);
        var doc = Doctor.Create(doctorUser.Id, spec.Id, "DOC-DENT-01", "BS. Răng");
        db.Doctors.Add(doc);
        var svc = Service.Create(spec.Id, "DV-RHM-01", "Cạo vôi răng", 30, 200_000m);
        db.Services.Add(svc);

        var apt1 = Appointment.CreateGuest(doc.Id, p1.Id, svc.Id, svc.Name, svc.DurationMinutes, svc.Price, DateTime.UtcNow.AddDays(1), "Khám 1");
        var apt2 = Appointment.CreateGuest(doc.Id, p2.Id, svc.Id, svc.Name, svc.DurationMinutes, svc.Price, DateTime.UtcNow.AddDays(2), "Khám 2");
        db.Appointments.AddRange(apt1, apt2);

        var reviewCase = PatientIdentityReviewCase.Create(p1.Id, p2.Id, IdentityMatchReason.NAME_DOB);
        db.PatientIdentityReviewCases.Add(reviewCase);
        await db.SaveChangesAsync();

        var staffUserId = Guid.NewGuid();
        var handler = new ResolvePatientReviewCaseCommandHandler(db, masking, NullLogger<ResolvePatientReviewCaseCommandHandler>.Instance);

        // Act: Lễ tân đối chiếu CCCD tại quầy và quyết định gộp P2 vào P1 (P1 là target, P2 là source)
        var result = await handler.Handle(new ResolvePatientReviewCaseCommand(
            CaseId: reviewCase.Id,
            Resolution: IdentityReviewStatus.CONFIRMED_SAME_PERSON,
            ReviewerUserId: staffUserId,
            Note: "Đã đối chiếu căn cước công dân gốc tại quầy, cùng 1 người",
            MergeReason: "Trùng số CCCD 001091001234",
            PreferredTargetPatientId: p1.Id
        ), CancellationToken.None);

        // Assert 1: Ca khám của P2 được chuyển sang P1
        result.IsSuccess.Should().BeTrue();
        var reloadedApt2 = await db.Appointments.FindAsync(apt2.Id);
        reloadedApt2!.PatientId.Should().Be(p1.Id);

        // Assert 2: P2 đổi trạng thái MERGED, P1 là ACTIVE
        var reloadedP2 = await db.Patients.FindAsync(p2.Id);
        reloadedP2!.RecordStatus.Should().Be("MERGED");
        reloadedP2.MergedIntoPatientId.Should().Be(p1.Id);

        // Assert 3: Tạo bản ghi PatientMergeEvent
        var mergeEvent = await db.PatientMergeEvents.FirstOrDefaultAsync(m => m.SourcePatientId == p2.Id && m.TargetPatientId == p1.Id);
        mergeEvent.Should().NotBeNull();
        mergeEvent!.Reason.Should().Be("Trùng số CCCD 001091001234");
        mergeEvent.BeforeSnapshotJson.Should().NotBeNullOrWhiteSpace();

        // Assert 4: Ghi nhận AuditLog với Action = MERGE_PATIENTS
        var auditLog = await db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "MERGE_PATIENTS" && a.EntityId == p1.Id);
        auditLog.Should().NotBeNull();
        auditLog!.ActorType.Should().Be("STAFF");
        auditLog.ActorUserId.Should().Be(staffUserId);
        auditLog.MetadataRedacted.Should().Contain(p2.PatientCode);
        auditLog.MetadataRedacted.Should().Contain(p1.PatientCode);
    }

    [Fact]
    public async Task MergeCaseAudited_WhenDifferentPerson_ClearsDuplicateFlagAndCreatesAuditLog()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (security, masking, _) = CreateServices(db);

        var p1 = Patient.Create("Đặng Thu Thảo", "0911000111", new DateOnly(1991, 2, 14), 1991);
        var p2 = Patient.Create("Đặng Thu Thảo", "0922000222", new DateOnly(1991, 2, 14), 1991);
        p1.IsPossibleDuplicate = true;
        p2.IsPossibleDuplicate = true;
        db.Patients.AddRange(p1, p2);

        var reviewCase = PatientIdentityReviewCase.Create(p1.Id, p2.Id, IdentityMatchReason.NAME_DOB);
        db.PatientIdentityReviewCases.Add(reviewCase);
        await db.SaveChangesAsync();

        var staffUserId = Guid.NewGuid();
        var handler = new ResolvePatientReviewCaseCommandHandler(db, masking, NullLogger<ResolvePatientReviewCaseCommandHandler>.Instance);

        // Act: Lễ tân đối chiếu CCCD và xác nhận là hai người khác nhau (trùng họ tên và ngày sinh ngẫu nhiên)
        var result = await handler.Handle(new ResolvePatientReviewCaseCommand(
            CaseId: reviewCase.Id,
            Resolution: IdentityReviewStatus.DIFFERENT_PERSON,
            ReviewerUserId: staffUserId,
            Note: "Đã kiểm tra CCCD, 2 người có quê quán và số CCCD hoàn toàn khác nhau"
        ), CancellationToken.None);

        // Assert 1: Ca rà soát cập nhật DIFFERENT_PERSON
        result.IsSuccess.Should().BeTrue();
        var reloadedCase = await db.PatientIdentityReviewCases.FindAsync(reviewCase.Id);
        reloadedCase!.Status.Should().Be(IdentityReviewStatus.DIFFERENT_PERSON);

        // Assert 2: Cờ nghi trùng được xóa
        var reloadedP1 = await db.Patients.FindAsync(p1.Id);
        var reloadedP2 = await db.Patients.FindAsync(p2.Id);
        reloadedP1!.IsPossibleDuplicate.Should().BeFalse();
        reloadedP2!.IsPossibleDuplicate.Should().BeFalse();

        // Assert 3: Ghi nhận AuditLog với Action = RESOLVE_PATIENT_DUPLICATE
        var auditLog = await db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "RESOLVE_PATIENT_DUPLICATE");
        auditLog.Should().NotBeNull();
        auditLog!.ActorType.Should().Be("STAFF");
        auditLog.ActorUserId.Should().Be(staffUserId);
        auditLog.MetadataRedacted.Should().Contain("DIFFERENT_PERSON");
    }

    [Fact]
    public async Task UpdatePatient_MergedPatient_CannotBeUpdated()
    {
        // Arrange: Hồ sơ đã MERGED
        using var db = CreateInMemoryDbContext();
        var (security, masking, _) = CreateServices(db);

        var patient = Patient.Create("Hồ Sơ Cũ", "0988000111", new DateOnly(1988, 3, 3), 1988);
        patient.RecordStatus = "MERGED";
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        var staff = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new UpdatePatientCommandHandler(db, staff, security, masking);

        // Act: Cố gắng sửa hồ sơ đã MERGED
        var result = await handler.Handle(new UpdatePatientCommand(
            PatientId: patient.Id,
            FullName: "Tên Mới",
            PhoneNumber: "0988000222",
            DateOfBirth: new DateOnly(1988, 3, 3)
        ), CancellationToken.None);

        // Assert: Bị từ chối với HTTP 400 Bad Request
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("Không thể chỉnh sửa hồ sơ đã được gộp");
    }
}
