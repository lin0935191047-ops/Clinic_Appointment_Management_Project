using ClinicAppointment.Api.Middlewares;
using ClinicAppointment.Application;
using ClinicAppointment.Infrastructure;
using ClinicAppointment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký các tầng kiến trúc Clean Architecture
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 2. Controller & OpenAPI / Swagger
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Clinic Appointment Management API",
        Version = "v1.2",
        Description = "Hệ thống quản lý và đặt lịch khám bệnh trực tuyến (ASP.NET Core 8 Web API & PostgreSQL)"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Nhập access token JWT: Bearer {token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 3. Cấu hình CORS (Hỗ trợ Local Dev, Vercel, Render và Cloudflare Tunnels cho Demo Online)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://+:{port}");
}
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? Array.Empty<string>();

        var defaultOrigins = new[] { "http://localhost:3000", "http://localhost:5173" };
        var allowedOrigins = defaultOrigins.Concat(configuredOrigins).ToHashSet(StringComparer.OrdinalIgnoreCase);

        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrWhiteSpace(origin)) return false;
            if (allowedOrigins.Contains(origin)) return true;
            if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            {
                return uri.Host.EndsWith("vercel.app", StringComparison.OrdinalIgnoreCase) ||
                       uri.Host.EndsWith("onrender.com", StringComparison.OrdinalIgnoreCase) ||
                       uri.Host.EndsWith("trycloudflare.com", StringComparison.OrdinalIgnoreCase) ||
                       uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

var app = builder.Build();

// 4. Cấu hình HTTP request pipeline
app.UseMiddleware<CorrelationAndLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

var enableSwagger = app.Environment.IsDevelopment() || 
                    builder.Configuration.GetValue<bool>("ENABLE_SWAGGER", true);

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Clinic Appointment API v1.2");
        c.RoutePrefix = "swagger";
    });
}

var autoMigrate = app.Environment.IsDevelopment() || 
                  builder.Configuration.GetValue<bool>("AUTO_MIGRATE", true);

if (autoMigrate)
{
    // Tự động migrate và seed nếu PostgreSQL kết nối thành công
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        if (db.Database.CanConnect())
        {
            await db.Database.MigrateAsync();
            await seeder.SeedAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Chưa thể kết nối tới cơ sở dữ liệu PostgreSQL để chạy migrate/seed tự động. Vui lòng kiểm tra mật khẩu kết nối.");
    }
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();
app.UseCors("DefaultCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
