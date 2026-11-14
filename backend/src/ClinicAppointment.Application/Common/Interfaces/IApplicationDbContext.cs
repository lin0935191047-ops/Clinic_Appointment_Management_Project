using ClinicAppointment.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Patient> Patients { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<Doctor> Doctors { get; }
    DbSet<Specialty> Specialties { get; }
    DbSet<Service> Services { get; }
    DbSet<DoctorSchedule> DoctorSchedules { get; }
    DbSet<RescheduleRequest> RescheduleRequests { get; }
    DbSet<Refund> Refunds { get; }
    DbSet<OtpChallenge> OtpChallenges { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

