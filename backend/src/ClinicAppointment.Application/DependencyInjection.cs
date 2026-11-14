using System.Reflection;
using ClinicAppointment.Application.Common.Behaviors;
using ClinicAppointment.Application.Features.Scheduling.Services;
using ClinicAppointment.Application.Features.TimeOffs.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicAppointment.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddValidatorsFromAssembly(assembly);
        
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        services.AddScoped<IDoctorTimeOffService, DoctorTimeOffService>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        services.AddScoped<ClinicAppointment.Application.Common.Interfaces.IOtpChallengeService, ClinicAppointment.Application.Features.Otp.Services.OtpChallengeService>();
        services.AddScoped<ClinicAppointment.Application.Common.Interfaces.IOtpGrantService, ClinicAppointment.Application.Features.Otp.Services.OtpGrantService>();
        services.AddSingleton<ClinicAppointment.Domain.Services.IPatientDuplicateDetectionService, ClinicAppointment.Domain.Services.PatientDuplicateDetectionService>();

        return services;
    }
}

