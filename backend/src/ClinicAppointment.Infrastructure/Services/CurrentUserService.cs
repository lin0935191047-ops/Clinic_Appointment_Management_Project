using System.Security.Claims;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace ClinicAppointment.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var idClaim = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.FindFirstValue("sub");
            return Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirstValue(ClaimTypes.Email) ?? User?.FindFirstValue("email");

    public IReadOnlyList<string> Roles => User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() 
        ?? (IReadOnlyList<string>)Array.Empty<string>();

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;

    public Guid? PatientProfileId
    {
        get
        {
            var claim = User?.FindFirstValue("patient_profile_id");
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public Guid? DoctorProfileId
    {
        get
        {
            var claim = User?.FindFirstValue("doctor_profile_id");
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public bool IsAdmin => IsInRole(Role.AdminCode);

    public bool IsStaff => IsInRole(Role.AdminCode) || IsInRole(Role.ReceptionistCode);

    public bool IsDoctor => IsInRole(Role.DoctorCode);

    public bool IsPatient => IsInRole(Role.PatientCode);
}
