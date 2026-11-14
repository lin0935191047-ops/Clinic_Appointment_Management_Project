using System.Text.Json;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Patients.DTOs;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Patients.Commands;

public record UpdatePatientCommand(
    Guid PatientId,
    string FullName,
    string PhoneNumber,
    DateOnly? DateOfBirth = null,
    short? BirthYear = null,
    string? Email = null,
    string? Gender = null,
    string? Address = null,
    string? NationalId = null,
    string? Passport = null,
    string? HealthInsurance = null
) : IRequest<Result<PatientDto>>;

public class UpdatePatientCommandValidator : AbstractValidator<UpdatePatientCommand>
{
    public UpdatePatientCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Mã định danh hồ sơ bệnh nhân là bắt buộc.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ và tên bệnh nhân không được để trống.")
            .MaximumLength(100).WithMessage("Họ và tên không được vượt quá 100 ký tự.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Số điện thoại liên hệ là bắt buộc.")
            .Matches(@"^[0-9]{9,15}$").WithMessage("Số điện thoại không đúng định dạng.");

        RuleFor(x => x)
            .Must(x => x.DateOfBirth.HasValue || x.BirthYear.HasValue)
            .WithMessage("Hồ sơ bệnh nhân bắt buộc phải có Ngày sinh hoặc Năm sinh.");

        When(x => x.DateOfBirth.HasValue && x.BirthYear.HasValue, () =>
        {
            RuleFor(x => x)
                .Must(x => x.DateOfBirth!.Value.Year == x.BirthYear!.Value)
                .WithMessage("Năm sinh và ngày sinh không khớp nhau.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email).EmailAddress().WithMessage("Email không hợp lệ.");
        });
    }
}

public class UpdatePatientCommandHandler : IRequestHandler<UpdatePatientCommand, Result<PatientDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPatientSecurityService _securityService;
    private readonly IIdentifierMaskingService _maskingService;

    public UpdatePatientCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPatientSecurityService securityService,
        IIdentifierMaskingService maskingService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _securityService = securityService;
        _maskingService = maskingService;
    }

    public async Task<Result<PatientDto>> Handle(
        UpdatePatientCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Phân quyền: Chỉ Staff (Receptionist / Admin) mới được sửa hồ sơ tại bàn tiếp nhận
        if (!_currentUserService.IsStaff)
        {
            return Result<PatientDto>.Failure(
                "Bạn không có quyền chỉnh sửa hồ sơ bệnh nhân. Yêu cầu quyền Receptionist hoặc Admin.", 
                403);
        }

        var staffUserId = _currentUserService.UserId ?? Guid.Empty;

        // 2. Tra cứu hồ sơ bệnh nhân
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken);

        if (patient == null)
        {
            return Result<PatientDto>.Failure("Không tìm thấy hồ sơ bệnh nhân cần chỉnh sửa.", 404);
        }

        if (patient.RecordStatus == "MERGED")
        {
            return Result<PatientDto>.Failure("Không thể chỉnh sửa hồ sơ đã được gộp vào hồ sơ khác.", 400);
        }

        // 3. Cập nhật thông tin cơ bản
        patient.UpdateDetails(
            fullName: request.FullName,
            phoneNumber: request.PhoneNumber,
            dateOfBirth: request.DateOfBirth,
            birthYear: request.BirthYear,
            email: request.Email,
            gender: request.Gender,
            address: request.Address
        );

        // 4. Cập nhật định danh mã hóa bảo mật nếu có cung cấp
        if (!string.IsNullOrWhiteSpace(request.NationalId))
        {
            _securityService.AttachNationalId(patient, request.NationalId);
        }

        if (!string.IsNullOrWhiteSpace(request.Passport))
        {
            _securityService.AttachPassport(patient, request.Passport);
        }

        if (!string.IsNullOrWhiteSpace(request.HealthInsurance))
        {
            _securityService.AttachInsuranceCard(patient, request.HealthInsurance);
        }

        // 5. Kiểm tra trùng lặp: Strictly NO Auto-merge
        // Quét ứng viên có cùng họ tên và ngày sinh/năm sinh trong hệ thống
        var normalizedName = patient.NormalizedFullName;
        var existingCandidates = await _context.Patients
            .Where(p => p.Id != patient.Id && p.RecordStatus == "ACTIVE" && p.NormalizedFullName == normalizedName)
            .Where(p => (patient.DateOfBirth.HasValue && p.DateOfBirth == patient.DateOfBirth) ||
                        (patient.BirthYear.HasValue && p.BirthYear == patient.BirthYear))
            .ToListAsync(cancellationToken);

        if (existingCandidates.Any())
        {
            // Đánh dấu cờ nghi trùng
            patient.IsPossibleDuplicate = true;

            foreach (var candidate in existingCandidates)
            {
                candidate.IsPossibleDuplicate = true;

                // Kiểm tra xem đã có ca rà soát OPEN giữa 2 hồ sơ này chưa
                var caseExists = await _context.PatientIdentityReviewCases.AnyAsync(c =>
                    ((c.PatientId == patient.Id && c.CandidatePatientId == candidate.Id) ||
                     (c.PatientId == candidate.Id && c.CandidatePatientId == patient.Id)) &&
                    c.Status == IdentityReviewStatus.OPEN, cancellationToken);

                if (!caseExists)
                {
                    var matchReason = (patient.DateOfBirth.HasValue && candidate.DateOfBirth == patient.DateOfBirth)
                        ? IdentityMatchReason.NAME_DOB
                        : IdentityMatchReason.NAME_BIRTH_YEAR;

                    var reviewCase = PatientIdentityReviewCase.Create(
                        patientId: patient.Id,
                        candidatePatientId: candidate.Id,
                        matchReason: matchReason
                    );

                    _context.PatientIdentityReviewCases.Add(reviewCase);
                }
            }
        }

        // 6. Ghi nhận AuditLog cho thao tác cập nhật hồ sơ
        var metadata = new
        {
            PatientId = patient.Id,
            PatientCode = patient.PatientCode,
            FullName = patient.FullName,
            DateOfBirth = patient.DateOfBirth,
            BirthYear = patient.BirthYear,
            HasNationalId = patient.NationalIdCiphertext != null,
            IsPossibleDuplicate = patient.IsPossibleDuplicate,
            Action = "UPDATE_PATIENT"
        };

        var auditLog = AuditLog.Create(
            actorType: "STAFF",
            actorUserId: staffUserId,
            action: "UPDATE_PATIENT",
            entityType: "Patient",
            entityId: patient.Id,
            metadataRedacted: JsonSerializer.Serialize(metadata)
        );

        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);

        // 7. Trả về DTO đã mã hóa/mask an toàn
        var decryptedCccd = _securityService.DecryptNationalId(patient);
        var decryptedPassport = _securityService.DecryptPassport(patient);
        var decryptedBhyt = _securityService.DecryptInsuranceCard(patient);

        var dto = new PatientDto
        {
            Id = patient.Id,
            UserId = patient.UserId,
            PatientCode = patient.PatientCode,
            FullName = patient.FullName,
            NormalizedFullName = patient.NormalizedFullName,
            DateOfBirth = patient.DateOfBirth,
            BirthYear = patient.BirthYear,
            PhoneNumber = patient.PhoneNumber,
            MaskedPhoneNumber = _maskingService.MaskPhone(patient.PhoneNumber),
            Email = patient.Email,
            Gender = patient.Gender,
            Address = patient.Address,
            MaskedNationalId = !string.IsNullOrWhiteSpace(decryptedCccd) ? _maskingService.MaskNationalId(decryptedCccd) : null,
            MaskedPassport = !string.IsNullOrWhiteSpace(decryptedPassport) ? _maskingService.MaskPassport(decryptedPassport) : null,
            MaskedHealthInsurance = !string.IsNullOrWhiteSpace(decryptedBhyt) ? _maskingService.MaskHealthInsurance(decryptedBhyt) : null,
            RecordStatus = patient.RecordStatus,
            IsPossibleDuplicate = patient.IsPossibleDuplicate,
            CreatedAt = patient.CreatedAt
        };

        return Result<PatientDto>.Success(dto, 200);
    }
}
