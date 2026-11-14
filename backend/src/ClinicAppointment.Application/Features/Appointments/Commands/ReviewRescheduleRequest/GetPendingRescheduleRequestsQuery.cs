using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Appointments.Commands.ReviewRescheduleRequest;

public record GetPendingRescheduleRequestsQuery : IRequest<Result<List<RescheduleRequestDetailDto>>>;

public class GetPendingRescheduleRequestsQueryHandler : IRequestHandler<GetPendingRescheduleRequestsQuery, Result<List<RescheduleRequestDetailDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetPendingRescheduleRequestsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<RescheduleRequestDetailDto>>> Handle(
        GetPendingRescheduleRequestsQuery request, 
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsStaff)
        {
            return Result<List<RescheduleRequestDetailDto>>.Failure(
                "Bạn không có quyền truy cập danh sách yêu cầu đổi lịch.", 
                403);
        }

        var requests = await _context.RescheduleRequests
            .Where(r => r.Status == RescheduleStatus.PendingReview)
            .OrderBy(r => r.CreatedAt)
            .Include(r => r.Appointment)
                .ThenInclude(a => a!.Patient)
            .Include(r => r.Appointment)
                .ThenInclude(a => a!.Doctor)
            .ToListAsync(cancellationToken);

        // Lấy danh sách RequestedDoctorId để nạp tên bác sĩ được yêu cầu
        var doctorIds = requests.Select(r => r.RequestedDoctorId).Distinct().ToList();
        var doctorsDict = await _context.Doctors
            .Where(d => doctorIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.FullName, cancellationToken);

        var dtos = requests.Select(r =>
        {
            var app = r.Appointment!;
            var requestedDoctorName = doctorsDict.TryGetValue(r.RequestedDoctorId, out var name)
                ? name
                : (app.Doctor?.FullName ?? "N/A");

            return new RescheduleRequestDetailDto(
                RequestId: r.Id,
                AppointmentId: r.AppointmentId,
                BookingCode: app.BookingCode,
                PatientFullName: app.Patient?.FullName ?? "N/A",
                PatientPhoneNumber: app.Patient?.PhoneNumber ?? "N/A",
                ServiceName: app.ServiceNameSnapshot,
                CurrentDoctorId: app.DoctorId,
                CurrentDoctorFullName: app.Doctor?.FullName ?? "N/A",
                CurrentStartAtUtc: app.StartAt,
                CurrentEndAtUtc: app.EndAt,
                RequestedDoctorId: r.RequestedDoctorId,
                RequestedDoctorFullName: requestedDoctorName,
                ProposedStartAtUtc: r.ProposedStartAt,
                ProposedEndAtUtc: r.ProposedEndAt,
                Reason: r.Reason,
                Status: r.Status,
                CreatedAtUtc: r.CreatedAt
            );
        }).ToList();

        return Result<List<RescheduleRequestDetailDto>>.Success(dtos, 200);
    }
}
