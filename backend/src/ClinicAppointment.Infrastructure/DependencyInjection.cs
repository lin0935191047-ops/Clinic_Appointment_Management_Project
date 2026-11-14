using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Domain.Services;
using ClinicAppointment.Infrastructure.Persistence;
using ClinicAppointment.Infrastructure.Security;
using ClinicAppointment.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        // Security & Policies
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IPiiEncryptionService, PiiEncryptionService>();
        services.AddSingleton<IHmacService, HmacService>();
        services.AddSingleton<IIdentifierMaskingService, IdentifierMaskingService>();
        services.AddScoped<IPatientSecurityService, PatientSecurityService>();
        services.AddScoped<IRoleAssignmentPolicy, RoleAssignmentPolicy>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
