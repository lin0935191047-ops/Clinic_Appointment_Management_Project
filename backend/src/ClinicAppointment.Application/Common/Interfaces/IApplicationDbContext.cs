using ClinicAppointment.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRoleAssignment> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Patient> Patients { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<Doctor> Doctors { get; }
    DbSet<Specialty> Specialties { get; }
    DbSet<Service> Services { get; }
    DbSet<DoctorSchedule> DoctorSchedules { get; }
    DbSet<DoctorTimeOff> DoctorTimeOffs { get; }
    DbSet<DoctorScheduleBlock> DoctorScheduleBlocks { get; }
    DbSet<RescheduleRequest> RescheduleRequests { get; }
    DbSet<Refund> Refunds { get; }
    DbSet<OtpChallenge> OtpChallenges { get; }
    DbSet<PatientIdentityReviewCase> PatientIdentityReviewCases { get; }
    DbSet<PatientMergeEvent> PatientMergeEvents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
