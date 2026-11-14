using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Patients.DTOs;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicAppointment.Application.Features.Patients.Commands;

public class CreatePatientCommandHandler : IRequestHandler<CreatePatientCommand, Result<PatientDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPatientSecurityService _securityService;
    private readonly IPatientDuplicateDetectionService _duplicateDetectionService;
    private readonly IIdentifierMaskingService _maskingService;
    private readonly ILogger<CreatePatientCommandHandler> _logger;

    public CreatePatientCommandHandler(
        IApplicationDbContext context,
        IPatientSecurityService securityService,
        IPatientDuplicateDetectionService duplicateDetectionService,
        IIdentifierMaskingService maskingService,
        ILogger<CreatePatientCommandHandler> logger)
    {
        _context = context;
        _securityService = securityService;
        _duplicateDetectionService = duplicateDetectionService;
        _maskingService = maskingService;
        _logger = logger;
    }

    public async Task<Result<PatientDto>> Handle(CreatePatientCommand request, CancellationToken cancellationToken)
    {
        // 1. Kiểm tra UserId nếu được chỉ định
        if (request.UserId.HasValue)
        {
            var existingWithUser = await _context.Patients
                .AnyAsync(p => p.UserId == request.UserId.Value, cancellationToken);

            if (existingWithUser)
            {
                return Result<PatientDto>.Failure(
                    "Tài khoản người dùng này đã được liên kết với một hồ sơ bệnh nhân khác.");
            }
        }

        // 2. Tạo thực thể Patient qua Factory Method
        var patient = Patient.Create(
            fullName: request.FullName,
            phoneNumber: request.PhoneNumber,
            dateOfBirth: request.DateOfBirth,
            birthYear: request.BirthYear,
            userId: request.UserId,
            email: request.Email,
            gender: request.Gender,
            address: request.Address
        );

        // 3. Cơ chế mã hóa và rà soát định danh nhạy cảm (FEAT-012)
        if (!string.IsNullOrWhiteSpace(request.NationalId))
        {
            await _securityService.CheckAndFlagDuplicateIdentifierAsync(patient, "NATIONAL_ID", request.NationalId, cancellationToken);
            _securityService.AttachNationalId(patient, request.NationalId);
        }

        if (!string.IsNullOrWhiteSpace(request.Passport))
        {
            await _securityService.CheckAndFlagDuplicateIdentifierAsync(patient, "PASSPORT", request.Passport, cancellationToken);
            _securityService.AttachPassport(patient, request.Passport);
        }

        if (!string.IsNullOrWhiteSpace(request.HealthInsuranceNumber))
        {
            _securityService.AttachInsuranceCard(patient, request.HealthInsuranceNumber);
        }

        if (!string.IsNullOrWhiteSpace(request.Pid))
        {
            _securityService.AttachPid(patient, request.Pid);
        }

        // 4. Cơ chế phát hiện hồ sơ nghi trùng theo nhân khẩu học (FEAT-013 - Strictly NO Auto-merge)
        var normalizedName = patient.NormalizedFullName;
        var phone = patient.PhoneNumber;

        var potentialCandidates = await _context.Patients
            .Where(p => p.RecordStatus != "MERGED" &&
                        (p.NormalizedFullName == normalizedName || p.PhoneNumber == phone))
            .Take(10)
            .ToListAsync(cancellationToken);

        var matches = _duplicateDetectionService.EvaluateCandidates(patient, potentialCandidates);
        var reviewCasesToCreate = new List<PatientIdentityReviewCase>();

        foreach (var match in matches)
        {
            if (match.RequiresManualReview)
            {
                patient.IsPossibleDuplicate = true;
                patient.RecordStatus = "REVIEW_REQUIRED";

                var reviewCase = PatientIdentityReviewCase.Create(
                    patientId: patient.Id,
                    candidatePatientId: match.ExistingPatient.Id,
                    matchReason: match.Reason
                );
                reviewCasesToCreate.Add(reviewCase);

                _logger.LogWarning(
                    "Tạo ca rà soát nghi trùng #{CaseId}: Bệnh nhân mới={NewCode} khớp với ứng viên={CandidateCode} vì lý do {Reason}. Không gộp tự động.",
                    reviewCase.Id, patient.PatientCode, match.ExistingPatient.PatientCode, match.Reason);
            }
            else
            {
                // Trường hợp SHARED_PHONE_DIFFERENT_NAME: An toàn số dùng chung gia đình, không gộp và không đánh dấu lỗi
                _logger.LogInformation(
                    "Số điện thoại {MaskedPhone} được dùng chung giữa các cá nhân khác nhau: '{Name1}' và '{Name2}'. Lưu trữ an toàn riêng biệt.",
                    _maskingService.MaskPhone(phone), patient.FullName, match.ExistingPatient.FullName);
            }
        }

        _context.Patients.Add(patient);
        if (reviewCasesToCreate.Any())
        {
            _context.PatientIdentityReviewCases.AddRange(reviewCasesToCreate);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Tạo hồ sơ bệnh nhân thành công: Mã={PatientCode}, Họ tên={FullName}, UserId={UserId}",
            patient.PatientCode, patient.FullName, patient.UserId?.ToString() ?? "Guest");

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
            MaskedNationalId = !string.IsNullOrWhiteSpace(request.NationalId) ? _maskingService.MaskNationalId(request.NationalId) : null,
            MaskedPassport = !string.IsNullOrWhiteSpace(request.Passport) ? _maskingService.MaskPassport(request.Passport) : null,
            MaskedHealthInsurance = !string.IsNullOrWhiteSpace(request.HealthInsuranceNumber) ? _maskingService.MaskHealthInsurance(request.HealthInsuranceNumber) : null,
            RecordStatus = patient.RecordStatus,
            IsPossibleDuplicate = patient.IsPossibleDuplicate,
            CreatedAt = patient.CreatedAt
        };

        return Result<PatientDto>.Success(dto);
    }
}
