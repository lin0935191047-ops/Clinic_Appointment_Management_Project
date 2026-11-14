using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Patients.DTOs;
using ClinicAppointment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicAppointment.Application.Features.Patients.Commands;

public class CreatePatientCommandHandler : IRequestHandler<CreatePatientCommand, Result<PatientDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPatientSecurityService _securityService;
    private readonly IIdentifierMaskingService _maskingService;
    private readonly ILogger<CreatePatientCommandHandler> _logger;

    public CreatePatientCommandHandler(
        IApplicationDbContext context,
        IPatientSecurityService securityService,
        IIdentifierMaskingService maskingService,
        ILogger<CreatePatientCommandHandler> logger)
    {
        _context = context;
        _securityService = securityService;
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

        // 4. Cơ chế phát hiện hồ sơ nghi trùng theo nhân khẩu học (Strictly NO Auto-merge)
        var normalizedName = patient.NormalizedFullName;
        var birthYear = patient.BirthYear;

        var duplicateCandidates = await _context.Patients
            .Where(p => p.RecordStatus != "MERGED" &&
                        p.NormalizedFullName == normalizedName &&
                        p.BirthYear == birthYear)
            .Take(5)
            .ToListAsync(cancellationToken);

        if (duplicateCandidates.Any())
        {
            patient.IsPossibleDuplicate = true;
            patient.RecordStatus = "REVIEW_REQUIRED";
            _logger.LogWarning(
                "Phát hiện {Count} hồ sơ nghi trùng với Họ tên '{FullName}' và Năm sinh '{BirthYear}'. Đánh dấu để Lễ tân/Quản trị viên rà soát thủ công.",
                duplicateCandidates.Count, patient.FullName, patient.BirthYear);
        }

        _context.Patients.Add(patient);
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
