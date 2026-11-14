using ClinicAppointment.Application.Features.TimeOffs.DTOs;
using FluentValidation;

namespace ClinicAppointment.Application.Features.TimeOffs.Validators;

public class CreateDoctorTimeOffRequestValidator : AbstractValidator<CreateDoctorTimeOffRequest>
{
    public CreateDoctorTimeOffRequestValidator()
    {
        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("Bác sĩ không được để trống.");

        RuleFor(x => x.StartAt)
            .LessThan(x => x.EndAt)
            .WithMessage("Thời điểm bắt đầu nghỉ phải trước thời điểm kết thúc.");
    }
}

public class CreateDoctorScheduleBlockRequestValidator : AbstractValidator<CreateDoctorScheduleBlockRequest>
{
    public CreateDoctorScheduleBlockRequestValidator()
    {
        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("Bác sĩ không được để trống.");

        RuleFor(x => x.StartAt)
            .LessThan(x => x.EndAt)
            .WithMessage("Thời điểm bắt đầu khung giờ bận phải trước thời điểm kết thúc.");
    }
}
