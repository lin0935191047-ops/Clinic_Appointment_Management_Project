using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Appointments.Commands.BookGuestAppointment;

public record BookGuestAppointmentResponse(
    Guid AppointmentId,
    string BookingCode,
    string PatientFullName,
    string ServiceName,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    AppointmentStatus Status);

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

    public BookGuestAppointmentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<BookGuestAppointmentResponse>> Handle(
        BookGuestAppointmentCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Kiểm tra bác sĩ và dịch vụ
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == request.DoctorId && d.IsActive, cancellationToken);
        if (doctor == null)
            return Result<BookGuestAppointmentResponse>.Failure("Bác sĩ không tồn tại hoặc đã ngừng nhận lịch.", 404);

        var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.IsActive, cancellationToken);
        if (service == null)
            return Result<BookGuestAppointmentResponse>.Failure("Dịch vụ khám không tồn tại hoặc tạm ngưng.", 404);

        // 2. Tạo hoặc liên kết hồ sơ Patient độc lập (UserId = null)
        var patient = Patient.Create(
            request.FullName, 
            request.PhoneNumber, 
            request.DateOfBirth, 
            request.BirthYear, 
            userId: null, 
            email: request.Email);

        _context.Patients.Add(patient);

        // 3. Khởi tạo Appointment kèm ServiceSnapshot
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

        var response = new BookGuestAppointmentResponse(
            appointment.Id,
            appointment.BookingCode,
            patient.FullName,
            appointment.ServiceNameSnapshot,
            appointment.StartAt,
            appointment.EndAt,
            appointment.Status);

        return Result<BookGuestAppointmentResponse>.Success(response);
    }
}

