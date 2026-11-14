using ClinicAppointment.Application.Common.Exceptions;
using ClinicAppointment.Application.Common.Helpers;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Domain.Services;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Appointments.Commands.BookGuestAppointment;

public record BookGuestAppointmentResponse(
    Guid AppointmentId,
    string BookingCode,
    string PatientFullName,
    string ServiceName,
    decimal ServicePrice,
    int ServiceDuration,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    AppointmentStatus Status,
    string NextSteps);

public record BookGuestAppointmentCommand(
    string FullName,
    string PhoneNumber,
    DateOnly? DateOfBirth,
    short? BirthYear,
    string? Email,
    Guid DoctorId,
    Guid ServiceId,
    DateTime StartAtUtc,
    string? ReasonForVisit) : IRequest<Result<BookGuestAppointmentResponse>>;

public class BookGuestAppointmentCommandValidator : AbstractValidator<BookGuestAppointmentCommand>
{
    public BookGuestAppointmentCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ và tên bệnh nhân là bắt buộc.")
            .MaximumLength(150).WithMessage("Họ tên không được vượt quá 150 ký tự.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Số điện thoại liên hệ là bắt buộc.")
            .Matches(@"^(0[3|5|7|8|9])[0-9]{8}$").WithMessage("Số điện thoại di động không hợp lệ.");

        RuleFor(x => x)
            .Must(x => x.DateOfBirth.HasValue || x.BirthYear.HasValue)
            .WithMessage("Hồ sơ bệnh nhân bắt buộc phải cung cấp Ngày sinh hoặc Năm sinh.");

        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("Vui lòng chọn bác sĩ khám.");

        RuleFor(x => x.ServiceId)
            .NotEmpty().WithMessage("Vui lòng chọn dịch vụ khám.");

        RuleFor(x => x.StartAtUtc)
            .GreaterThan(DateTime.UtcNow).WithMessage("Thời gian hẹn khám phải ở tương lai.");
    }
}

public class BookGuestAppointmentCommandHandler : IRequestHandler<BookGuestAppointmentCommand, Result<BookGuestAppointmentResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPatientDuplicateDetectionService _duplicateDetectionService;

    public BookGuestAppointmentCommandHandler(
        IApplicationDbContext context,
        IPatientDuplicateDetectionService duplicateDetectionService)
    {
        _context = context;
        _duplicateDetectionService = duplicateDetectionService;
    }

    public async Task<Result<BookGuestAppointmentResponse>> Handle(
        BookGuestAppointmentCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Kiểm tra bác sĩ và chuyên khoa hoạt động
        var doctor = await _context.Doctors
            .Include(d => d.Specialty)
            .FirstOrDefaultAsync(d => d.Id == request.DoctorId && d.IsActive, cancellationToken);

        if (doctor == null)
            return Result<BookGuestAppointmentResponse>.Failure("Bác sĩ không tồn tại hoặc đã ngừng nhận lịch.", 404);

        if (doctor.Specialty != null && !doctor.Specialty.IsActive)
            return Result<BookGuestAppointmentResponse>.Failure("Chuyên khoa của bác sĩ hiện đang tạm ngưng hoạt động.", 400);

        // 2. Kiểm tra dịch vụ hoạt động
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.IsActive, cancellationToken);

        if (service == null)
            return Result<BookGuestAppointmentResponse>.Failure("Dịch vụ khám không tồn tại hoặc tạm ngưng.", 404);

        // 3. Quy đổi thời gian sang giờ địa phương Việt Nam và tính khoảng thời gian
        var endAtUtc = request.StartAtUtc.AddMinutes(service.DurationMinutes);
        var localStart = TimeZoneHelper.ToLocal(request.StartAtUtc);
        var localEnd = TimeZoneHelper.ToLocal(endAtUtc);
        var localDate = DateOnly.FromDateTime(localStart);
        var localStartTime = TimeOnly.FromDateTime(localStart);
        var localEndTime = TimeOnly.FromDateTime(localEnd);

        // Thứ trong tuần theo chuẩn ISO 8601 (1 = Thứ Hai, ..., 7 = Chủ Nhật)
        var dayOfWeekIso = TimeZoneHelper.GetIsoDayOfWeek(localDate);

        // 4. Kiểm tra ca làm việc của bác sĩ (DoctorSchedule)
        var schedules = await _context.DoctorSchedules
            .Where(s => s.DoctorId == doctor.Id &&
                        s.DayOfWeek == dayOfWeekIso &&
                        s.IsActive &&
                        s.EffectiveFrom <= localDate &&
                        (!s.EffectiveTo.HasValue || s.EffectiveTo.Value >= localDate))
            .ToListAsync(cancellationToken);

        bool isInShift = schedules.Any(s => s.StartTime <= localStartTime && s.EndTime >= localEndTime);
        if (!isInShift)
        {
            return Result<BookGuestAppointmentResponse>.Failure("Khung giờ hẹn không nằm trong ca làm việc của bác sĩ.", 400);
        }

        // 5. Kiểm tra xung đột ngày nghỉ được duyệt (DoctorTimeOff)
        bool hasTimeOff = await _context.DoctorTimeOffs
            .AnyAsync(t => t.DoctorId == doctor.Id &&
                           t.Status == TimeOffStatus.Active &&
                           t.StartAt < endAtUtc &&
                           t.EndAt > request.StartAtUtc, cancellationToken);

        if (hasTimeOff)
        {
            throw new AppointmentConflictException("Bác sĩ có lịch nghỉ đã được duyệt trong khung giờ này.");
        }

        // 6. Kiểm tra xung đột khóa lịch / bận (DoctorScheduleBlock)
        bool hasBlock = await _context.DoctorScheduleBlocks
            .AnyAsync(b => b.DoctorId == doctor.Id &&
                           b.StartAt < endAtUtc &&
                           b.EndAt > request.StartAtUtc, cancellationToken);

        if (hasBlock)
        {
            throw new AppointmentConflictException("Khung giờ này đã bị khóa hoặc bác sĩ có lịch bận đột xuất.");
        }

        // 7. Kiểm tra trùng lịch hẹn đang hoạt động (Active Appointments)
        bool hasOverlap = await _context.Appointments
            .AnyAsync(a => a.DoctorId == doctor.Id &&
                           a.Status != AppointmentStatus.Cancelled &&
                           a.StartAt < endAtUtc &&
                           a.EndAt > request.StartAtUtc, cancellationToken);

        if (hasOverlap)
        {
            throw new AppointmentConflictException("Khung giờ đã có lịch hẹn khác. Vui lòng chọn khung giờ khác.");
        }

        // 8. Thực hiện giao dịch lưu trữ (Transaction)
        bool supportsTransaction = _context.Database.ProviderName != null && 
                                   !_context.Database.ProviderName.Contains("InMemory", StringComparison.OrdinalIgnoreCase);
        
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
        if (supportsTransaction)
        {
            transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            // Hồ sơ Patient độc lập: UserId = null
            var patient = Patient.Create(
                request.FullName,
                request.PhoneNumber,
                request.DateOfBirth,
                request.BirthYear,
                userId: null,
                email: request.Email);

            _context.Patients.Add(patient);

            // Nghiệp vụ rà soát trùng lặp hồ sơ (Strictly NO Auto-merge):
            // Lấy các hồ sơ có thể trùng tên hoặc số điện thoại trong DB
            var candidates = await _context.Patients
                .Where(p => p.NormalizedFullName == patient.NormalizedFullName || p.PhoneNumber == patient.PhoneNumber)
                .ToListAsync(cancellationToken);

            var matches = _duplicateDetectionService.EvaluateCandidates(patient, candidates);
            var reviewCandidate = matches.FirstOrDefault(m => m.RequiresManualReview);

            if (reviewCandidate != null)
            {
                var reviewCase = PatientIdentityReviewCase.Create(
                    patient.Id,
                    reviewCandidate.ExistingPatient.Id,
                    reviewCandidate.Reason);

                _context.PatientIdentityReviewCases.Add(reviewCase);
            }

            // Khởi tạo cuộc hẹn với snapshot dịch vụ tại thời điểm đặt
            var appointment = Appointment.CreateGuest(
                doctor.Id,
                patient.Id,
                service.Id,
                service.Name,
                service.DurationMinutes,
                service.Price,
                request.StartAtUtc,
                request.ReasonForVisit);

            _context.Appointments.Add(appointment);

            await _context.SaveChangesAsync(cancellationToken);

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            var response = new BookGuestAppointmentResponse(
                appointment.Id,
                appointment.BookingCode,
                patient.FullName,
                appointment.ServiceNameSnapshot,
                appointment.ServicePriceSnapshot,
                appointment.ServiceDurationSnapshot,
                appointment.StartAt,
                appointment.EndAt,
                appointment.Status,
                "Quý khách vui lòng lưu lại Mã đặt lịch (Booking Code) và đến trước giờ hẹn 15 phút để hoàn tất thủ tục tiếp đón.");

            return Result<BookGuestAppointmentResponse>.Success(response, 201);
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }
}
