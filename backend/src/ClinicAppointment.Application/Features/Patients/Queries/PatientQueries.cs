using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Patients.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Patients.Queries;

public record GetPatientByIdQuery(Guid Id) : IRequest<Result<PatientDto>>;

public class GetPatientByIdQueryHandler : IRequestHandler<GetPatientByIdQuery, Result<PatientDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPatientByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
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
            Email = patient.Email,
            Gender = patient.Gender,
            Address = patient.Address,
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

    public GetPatientByCodeQueryHandler(IApplicationDbContext context)
    {
        _context = context;
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
            Email = patient.Email,
            Gender = patient.Gender,
            Address = patient.Address,
            RecordStatus = patient.RecordStatus,
            IsPossibleDuplicate = patient.IsPossibleDuplicate,
            CreatedAt = patient.CreatedAt
        };

        return Result<PatientDto>.Success(dto);
    }
}
