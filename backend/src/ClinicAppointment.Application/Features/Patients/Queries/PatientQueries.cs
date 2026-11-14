using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Patients.DTOs;
using ClinicAppointment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Patients.Queries;

public record GetPatientByIdQuery(Guid Id) : IRequest<Result<PatientDto>>;

public class GetPatientByIdQueryHandler : IRequestHandler<GetPatientByIdQuery, Result<PatientDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPatientSecurityService _securityService;
    private readonly IIdentifierMaskingService _maskingService;

    public GetPatientByIdQueryHandler(
        IApplicationDbContext context,
        IPatientSecurityService securityService,
        IIdentifierMaskingService maskingService)
    {
        _context = context;
        _securityService = securityService;
        _maskingService = maskingService;
    }

    public async Task<Result<PatientDto>> Handle(GetPatientByIdQuery request, CancellationToken cancellationToken)
    {
        var patient = await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (patient == null)
        {
            return Result<PatientDto>.Failure("Không tìm thấy hồ sơ bệnh nhân.");
        }

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

        return Result<PatientDto>.Success(dto);
    }
}

public record GetPatientByCodeQuery(string PatientCode) : IRequest<Result<PatientDto>>;

public class GetPatientByCodeQueryHandler : IRequestHandler<GetPatientByCodeQuery, Result<PatientDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPatientSecurityService _securityService;
    private readonly IIdentifierMaskingService _maskingService;

    public GetPatientByCodeQueryHandler(
        IApplicationDbContext context,
        IPatientSecurityService securityService,
        IIdentifierMaskingService maskingService)
    {
        _context = context;
        _securityService = securityService;
        _maskingService = maskingService;
    }

    public async Task<Result<PatientDto>> Handle(GetPatientByCodeQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PatientCode))
        {
            return Result<PatientDto>.Failure("Mã hồ sơ bệnh nhân không được để trống.");
        }

        var patient = await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PatientCode == request.PatientCode.Trim(), cancellationToken);

        if (patient == null)
        {
            return Result<PatientDto>.Failure($"Không tìm thấy hồ sơ bệnh nhân với mã '{request.PatientCode}'.");
        }

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

        return Result<PatientDto>.Success(dto);
    }
}

public record LookupPatientByIdentifierQuery(string IdentifierType, string Value) : IRequest<Result<PatientDto>>;

public class LookupPatientByIdentifierQueryHandler : IRequestHandler<LookupPatientByIdentifierQuery, Result<PatientDto>>
{
    private readonly IPatientSecurityService _securityService;
    private readonly IIdentifierMaskingService _maskingService;

    public LookupPatientByIdentifierQueryHandler(
        IPatientSecurityService securityService,
        IIdentifierMaskingService maskingService)
    {
        _securityService = securityService;
        _maskingService = maskingService;
    }

    public async Task<Result<PatientDto>> Handle(LookupPatientByIdentifierQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Value))
        {
            return Result<PatientDto>.Failure("Giá trị định danh cần tra cứu không được để trống.");
        }

        Patient? patient = request.IdentifierType.ToUpperInvariant() switch
        {
            "NATIONAL_ID" or "CCCD" => await _securityService.FindByNationalIdAsync(request.Value, cancellationToken),
            "PASSPORT" or "HO_CHIEU" => await _securityService.FindByPassportAsync(request.Value, cancellationToken),
            "HEALTH_INSURANCE" or "BHYT" => await _securityService.FindByInsuranceCardAsync(request.Value, cancellationToken),
            _ => null
        };

        if (patient == null)
        {
            return Result<PatientDto>.Failure("Không tìm thấy hồ sơ bệnh nhân khớp với định danh cung cấp.", 404);
        }

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

        return Result<PatientDto>.Success(dto);
    }
}
