using ClinicAppointment.Application.Features.Schedules.DTOs;
using FluentValidation;

namespace ClinicAppointment.Application.Features.Schedules.Validators;

public class CreateDoctorScheduleRequestValidator : AbstractValidator<CreateDoctorScheduleRequest>
{
    public CreateDoctorScheduleRequestValidator()
    {
        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("Bác sĩ không được để trống.");

        RuleFor(x => x.DayOfWeek)
            .InclusiveBetween((short)1, (short)7)
            .WithMessage("Thứ trong tuần phải từ 1 (Thứ Hai) đến 7 (Chủ Nhật).");

        RuleFor(x => x.StartTime)
            .LessThan(x => x.EndTime)
            .WithMessage("Giờ bắt đầu ca làm việc phải trước giờ kết thúc.");

        RuleFor(x => x.SlotDurationMinutes)
            .GreaterThan(0)
            .WithMessage("Thời lượng mỗi slot phải lớn hơn 0 phút.");

        RuleFor(x => x)
            .Must(x => !x.EffectiveTo.HasValue || x.EffectiveFrom <= x.EffectiveTo.Value)
            .WithMessage("Ngày bắt đầu hiệu lực không được lớn hơn ngày kết thúc hiệu lực.")
            .WithName("EffectiveFrom");
    }
}

public class UpdateDoctorScheduleRequestValidator : AbstractValidator<UpdateDoctorScheduleRequest>
{
    public UpdateDoctorScheduleRequestValidator()
    {
        RuleFor(x => x.DayOfWeek)
            .InclusiveBetween((short)1, (short)7)
            .WithMessage("Thứ trong tuần phải từ 1 (Thứ Hai) đến 7 (Chủ Nhật).");

        RuleFor(x => x.StartTime)
            .LessThan(x => x.EndTime)
            .WithMessage("Giờ bắt đầu ca làm việc phải trước giờ kết thúc.");

        RuleFor(x => x.SlotDurationMinutes)
            .GreaterThan(0)
            .WithMessage("Thời lượng mỗi slot phải lớn hơn 0 phút.");

        RuleFor(x => x)
            .Must(x => !x.EffectiveTo.HasValue || x.EffectiveFrom <= x.EffectiveTo.Value)
            .WithMessage("Ngày bắt đầu hiệu lực không được lớn hơn ngày kết thúc hiệu lực.")
            .WithName("EffectiveFrom");
    }
}
