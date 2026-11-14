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
builder.Services.AddControllers();
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

// 3. Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// 4. Cấu hình HTTP request pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Clinic Appointment API v1.2");
        c.RoutePrefix = "swagger";
    });

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

app.UseHttpsRedirection();
app.UseCors("DefaultCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
