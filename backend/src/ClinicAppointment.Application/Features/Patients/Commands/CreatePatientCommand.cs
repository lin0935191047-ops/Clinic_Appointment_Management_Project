using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Patients.DTOs;
using MediatR;

namespace ClinicAppointment.Application.Features.Patients.Commands;

public record CreatePatientCommand(
    string FullName,
    string PhoneNumber,
    DateOnly? DateOfBirth = null,
    short? BirthYear = null,
    Guid? UserId = null,
    string? Email = null,
    string? Gender = null,
    string? Address = null,
    string? NationalId = null,
    string? Passport = null,
    string? HealthInsuranceNumber = null,
    string? Pid = null
) : IRequest<Result<PatientDto>>;
