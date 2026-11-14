using System.Text;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Notifications;
using ClinicAppointment.Application.Features.Notifications.Services;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Services;
using ClinicAppointment.Infrastructure.Persistence;
using ClinicAppointment.Infrastructure.Security;
using ClinicAppointment.Infrastructure.Services;
using ClinicAppointment.Infrastructure.Services.Notifications;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ClinicAppointment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? configuration["DB_CONNECTION_STRING"]
            ?? "Host=localhost;Port=5432;Database=ClinicAppointmentDb;Username=postgres;Password=postgres;";

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IApplicationDbContext>(provider => 
            provider.GetRequiredService<ApplicationDbContext>());

        // Security, JWT & Policies
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false; // Local development
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IPiiEncryptionService, PiiEncryptionService>();
        services.AddSingleton<IHmacService, HmacService>();
        services.AddSingleton<IIdentifierMaskingService, IdentifierMaskingService>();
        services.AddScoped<IPatientSecurityService, PatientSecurityService>();
        services.AddSingleton<IPatientDuplicateDetectionService, PatientDuplicateDetectionService>();
        services.AddScoped<IRoleAssignmentPolicy, RoleAssignmentPolicy>();
        services.AddScoped<DatabaseSeeder>();

        // Logging & PII Redaction
        services.AddSingleton<ILogRedactor, Logging.LogRedactor>();

        // Health Checks
        services.AddHealthChecks()
            .AddCheck<Health.DatabaseHealthCheck>("database_health_check", tags: new[] { "ready", "db" });

        // Abuse Protection & Rate Limiting
        services.AddMemoryCache();
        services.AddSingleton<IAbuseProtectionService, AbuseProtectionService>();
        services.AddSingleton<ISmsSender, MockSmsSender>();

        // Notification Services & Outbox Abstractions (TBD Providers)
        services.AddSingleton<INotificationSafetyGuard, NotificationSafetyGuard>();
        services.AddScoped<INotificationChannel, DevStubSmsChannel>();
        services.AddScoped<INotificationChannel, DevStubEmailChannel>();
        services.AddScoped<INotificationChannel, DevStubInAppChannel>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAppointmentNotificationService, AppointmentNotificationService>();

        // Current User & Resource Authorization
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IResourceAuthorizationService, ResourceAuthorizationService>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole(Role.AdminCode));
            options.AddPolicy("StaffOnly", policy => policy.RequireRole(Role.AdminCode, Role.ReceptionistCode));
            options.AddPolicy("DoctorOnly", policy => policy.RequireRole(Role.DoctorCode));
            options.AddPolicy("PatientOnly", policy => policy.RequireRole(Role.PatientCode));
            options.AddPolicy("StaffOrDoctor", policy => policy.RequireRole(Role.AdminCode, Role.ReceptionistCode, Role.DoctorCode));
        });

        return services;
    }
}
