using ClinicAppointment.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Infrastructure.Services;

public class ResourceAuthorizationService : IResourceAuthorizationService
{
    private readonly ICurrentUserService _currentUser;
    private readonly IApplicationDbContext _context;

    public ResourceAuthorizationService(
        ICurrentUserService currentUser,
        IApplicationDbContext context)
    {
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<bool> CanAccessPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated)
            return false;

        // Staff (Admin, Receptionist) có toàn quyền quản lý hồ sơ bệnh nhân
        if (_currentUser.IsStaff)
            return true;

        // Bệnh nhân chỉ được xem hồ sơ của chính mình (chống IDOR)
        if (_currentUser.IsPatient)
        {
            if (_currentUser.PatientProfileId.HasValue && _currentUser.PatientProfileId.Value == patientId)
                return true;

            if (_currentUser.UserId.HasValue)
            {
                return await _context.Patients
                    .AnyAsync(p => p.Id == patientId && p.UserId == _currentUser.UserId.Value, cancellationToken);
            }
        }

        return false;
    }

    public async Task<bool> CanAccessAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated)
            return false;

        // Staff (Admin, Receptionist) có toàn quyền điều phối lịch khám
        if (_currentUser.IsStaff)
            return true;

        var appointment = await _context.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment == null)
            return false;

        // Bác sĩ chỉ được xem ca khám được phân công cho chính mình
        if (_currentUser.IsDoctor)
        {
            if (_currentUser.DoctorProfileId.HasValue && appointment.DoctorId == _currentUser.DoctorProfileId.Value)
                return true;

            if (_currentUser.UserId.HasValue)
            {
                return await _context.Doctors
                    .AnyAsync(d => d.Id == appointment.DoctorId && d.UserId == _currentUser.UserId.Value, cancellationToken);
            }

            return false;
        }

        // Bệnh nhân chỉ được xem ca khám của chính mình
        if (_currentUser.IsPatient)
        {
            if (_currentUser.PatientProfileId.HasValue && appointment.PatientId == _currentUser.PatientProfileId.Value)
                return true;

            if (_currentUser.UserId.HasValue)
            {
                return await _context.Patients
                    .AnyAsync(p => p.Id == appointment.PatientId && p.UserId == _currentUser.UserId.Value, cancellationToken);
            }

            return false;
        }

        return false;
    }

    public async Task<bool> CanAccessDoctorScheduleAsync(Guid doctorId, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated)
            return false;

        // Quản trị viên và Lễ tân có quyền quản lý lịch của phòng khám
        if (_currentUser.IsStaff)
            return true;

        // Bác sĩ chỉ có quyền xem/quản lý lịch làm việc của chính mình
        if (_currentUser.IsDoctor)
        {
            if (_currentUser.DoctorProfileId.HasValue && _currentUser.DoctorProfileId.Value == doctorId)
                return true;

            if (_currentUser.UserId.HasValue)
            {
                return await _context.Doctors
                    .AnyAsync(d => d.Id == doctorId && d.UserId == _currentUser.UserId.Value, cancellationToken);
            }
        }

        return false;
    }
}
