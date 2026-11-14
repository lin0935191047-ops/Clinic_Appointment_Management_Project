using System.Text.Json;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Patients.DTOs;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicAppointment.Application.Features.Patients.Commands;

public record ResolvePatientReviewCaseCommand(
    Guid CaseId,
    IdentityReviewStatus Resolution,
    Guid ReviewerUserId,
    string Note,
    string? MergeReason = null,
    Guid? PreferredTargetPatientId = null
) : IRequest<Result<PatientIdentityReviewCaseDto>>;

public class ResolvePatientReviewCaseCommandHandler : IRequestHandler<ResolvePatientReviewCaseCommand, Result<PatientIdentityReviewCaseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentifierMaskingService _maskingService;
    private readonly ILogger<ResolvePatientReviewCaseCommandHandler> _logger;

    public ResolvePatientReviewCaseCommandHandler(
        IApplicationDbContext context,
        IIdentifierMaskingService maskingService,
        ILogger<ResolvePatientReviewCaseCommandHandler> logger)
    {
        _context = context;
        _maskingService = maskingService;
        _logger = logger;
    }

    public async Task<Result<PatientIdentityReviewCaseDto>> Handle(
        ResolvePatientReviewCaseCommand request, 
        CancellationToken cancellationToken)
    {
        var reviewCase = await _context.PatientIdentityReviewCases
            .Include(c => c.Patient)
            .Include(c => c.CandidatePatient)
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, cancellationToken);

        if (reviewCase == null)
        {
            return Result<PatientIdentityReviewCaseDto>.Failure("Không tìm thấy ca rà soát hồ sơ trùng lặp.", 404);
        }

        if (reviewCase.Status != IdentityReviewStatus.OPEN && reviewCase.Status != IdentityReviewStatus.NEEDS_MORE_INFO)
        {
            return Result<PatientIdentityReviewCaseDto>.Failure("Ca rà soát này đã được kết luận trước đó.");
        }

        // 1. Phê duyệt gộp hồ sơ: CONFIRMED_SAME_PERSON
        if (request.Resolution == IdentityReviewStatus.CONFIRMED_SAME_PERSON)
        {
            if (string.IsNullOrWhiteSpace(request.MergeReason))
            {
                return Result<PatientIdentityReviewCaseDto>.Failure("Lý do gộp hồ sơ là bắt buộc khi kết luận cùng một người.");
            }

            // Xác định hồ sơ nguồn (source - bị gộp) và hồ sơ đích (target - được giữ)
            var targetPatient = (request.PreferredTargetPatientId == reviewCase.PatientId)
                ? reviewCase.Patient
                : reviewCase.CandidatePatient;

            var sourcePatient = (targetPatient.Id == reviewCase.PatientId)
                ? reviewCase.CandidatePatient
                : reviewCase.Patient;

            // Chụp snapshot trước khi gộp để lưu audit
            var beforeSnapshot = new
            {
                sourcePatient.Id,
                sourcePatient.PatientCode,
                sourcePatient.FullName,
                MaskedPhone = _maskingService.MaskPhone(sourcePatient.PhoneNumber),
                sourcePatient.DateOfBirth,
                sourcePatient.BirthYear,
                sourcePatient.RecordStatus,
                AppointmentsCount = await _context.Appointments.CountAsync(a => a.PatientId == sourcePatient.Id, cancellationToken)
            };

            var snapshotJson = JsonSerializer.Serialize(beforeSnapshot);

            // Chuyển toàn bộ lịch khám của hồ sơ nguồn sang hồ sơ đích
            var appointmentsToMigrate = await _context.Appointments
                .Where(a => a.PatientId == sourcePatient.Id)
                .ToListAsync(cancellationToken);

            foreach (var apt in appointmentsToMigrate)
            {
                apt.PatientId = targetPatient.Id;
            }

            // Đánh dấu hồ sơ nguồn là MERGED
            sourcePatient.RecordStatus = "MERGED";
            sourcePatient.MergedIntoPatientId = targetPatient.Id;
            sourcePatient.IsPossibleDuplicate = false;

            // Hồ sơ đích giữ nguyên hoặc chuyển ACTIVE
            targetPatient.RecordStatus = "ACTIVE";
            targetPatient.IsPossibleDuplicate = false;

            // Tạo bản ghi kiểm toán gộp hồ sơ
            var mergeEvent = PatientMergeEvent.Create(
                sourcePatientId: sourcePatient.Id,
                targetPatientId: targetPatient.Id,
                mergedByUserId: request.ReviewerUserId,
                reason: request.MergeReason,
                beforeSnapshotJson: snapshotJson
            );

            _context.PatientMergeEvents.Add(mergeEvent);

            // Ghi nhận AuditLog cho sự kiện gộp hồ sơ
            var mergeMetadata = new
            {
                SourcePatientId = sourcePatient.Id,
                SourcePatientCode = sourcePatient.PatientCode,
                TargetPatientId = targetPatient.Id,
                TargetPatientCode = targetPatient.PatientCode,
                Reason = request.MergeReason,
                AppointmentsMigratedCount = appointmentsToMigrate.Count,
                ReviewerNote = request.Note
            };

            var mergeAuditLog = AuditLog.Create(
                actorType: "STAFF",
                actorUserId: request.ReviewerUserId,
                action: "MERGE_PATIENTS",
                entityType: "Patient",
                entityId: targetPatient.Id,
                metadataRedacted: JsonSerializer.Serialize(mergeMetadata)
            );
            _context.AuditLogs.Add(mergeAuditLog);

            // Cập nhật trạng thái ca rà soát
            reviewCase.Resolve(IdentityReviewStatus.CONFIRMED_SAME_PERSON, request.ReviewerUserId, request.Note);

            _logger.LogInformation(
                "Đã thực hiện gộp hồ sơ thành công: Nguồn={SourceCode} -> Đích={TargetCode}, Người duyệt={UserId}, Lý do={Reason}",
                sourcePatient.PatientCode, targetPatient.PatientCode, request.ReviewerUserId, request.MergeReason);
        }
        // 2. Kết luận là hai người khác nhau: DIFFERENT_PERSON
        else if (request.Resolution == IdentityReviewStatus.DIFFERENT_PERSON)
        {
            reviewCase.Resolve(IdentityReviewStatus.DIFFERENT_PERSON, request.ReviewerUserId, request.Note);

            // Ghi nhận AuditLog cho sự kiện xác nhận hai người khác nhau
            var diffMetadata = new
            {
                CaseId = reviewCase.Id,
                Resolution = "DIFFERENT_PERSON",
                PatientCode = reviewCase.Patient.PatientCode,
                CandidatePatientCode = reviewCase.CandidatePatient.PatientCode,
                Note = request.Note
            };

            var diffAuditLog = AuditLog.Create(
                actorType: "STAFF",
                actorUserId: request.ReviewerUserId,
                action: "RESOLVE_PATIENT_DUPLICATE",
                entityType: "Patient",
                entityId: reviewCase.PatientId,
                metadataRedacted: JsonSerializer.Serialize(diffMetadata)
            );
            _context.AuditLogs.Add(diffAuditLog);

            // Xóa cờ nghi trùng nếu không còn ca rà soát nào khác đang OPEN
            var otherOpenCasesForPatient = await _context.PatientIdentityReviewCases
                .AnyAsync(c => c.Id != reviewCase.Id && 
                               (c.PatientId == reviewCase.PatientId || c.CandidatePatientId == reviewCase.PatientId) && 
                               c.Status == IdentityReviewStatus.OPEN, cancellationToken);

            if (!otherOpenCasesForPatient)
            {
                reviewCase.Patient.IsPossibleDuplicate = false;
                reviewCase.Patient.RecordStatus = "ACTIVE";
            }

            var otherOpenCasesForCandidate = await _context.PatientIdentityReviewCases
                .AnyAsync(c => c.Id != reviewCase.Id && 
                               (c.PatientId == reviewCase.CandidatePatientId || c.CandidatePatientId == reviewCase.CandidatePatientId) && 
                               c.Status == IdentityReviewStatus.OPEN, cancellationToken);

            if (!otherOpenCasesForCandidate)
            {
                reviewCase.CandidatePatient.IsPossibleDuplicate = false;
                reviewCase.CandidatePatient.RecordStatus = "ACTIVE";
            }

            _logger.LogInformation(
                "Đã xác nhận là hai người khác biệt: Hồ sơ 1={Code1}, Hồ sơ 2={Code2}, Người duyệt={UserId}",
                reviewCase.Patient.PatientCode, reviewCase.CandidatePatient.PatientCode, request.ReviewerUserId);
        }
        // 3. Yêu cầu thêm thông tin khi bệnh nhân tới quầy: NEEDS_MORE_INFO
        else if (request.Resolution == IdentityReviewStatus.NEEDS_MORE_INFO)
        {
            reviewCase.Status = IdentityReviewStatus.NEEDS_MORE_INFO;
            reviewCase.ReviewedByUserId = request.ReviewerUserId;
            reviewCase.ReviewedAt = DateTime.UtcNow;
            reviewCase.ReviewNote = request.Note;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new PatientIdentityReviewCaseDto
        {
            Id = reviewCase.Id,
            PatientId = reviewCase.PatientId,
            PatientCode = reviewCase.Patient.PatientCode,
            PatientFullName = reviewCase.Patient.FullName,
            PatientMaskedPhone = _maskingService.MaskPhone(reviewCase.Patient.PhoneNumber),
            CandidatePatientId = reviewCase.CandidatePatientId,
            CandidatePatientCode = reviewCase.CandidatePatient.PatientCode,
            CandidatePatientFullName = reviewCase.CandidatePatient.FullName,
            CandidateMaskedPhone = _maskingService.MaskPhone(reviewCase.CandidatePatient.PhoneNumber),
            MatchReason = reviewCase.MatchReason.ToString(),
            Status = reviewCase.Status.ToString(),
            ReviewedByUserId = reviewCase.ReviewedByUserId,
            ReviewedAt = reviewCase.ReviewedAt,
            ReviewNote = reviewCase.ReviewNote,
            CreatedAt = reviewCase.CreatedAt
        };

        return Result<PatientIdentityReviewCaseDto>.Success(dto);
    }
}
