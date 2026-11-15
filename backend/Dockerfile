# STAGE 1: Build & Publish .NET Backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file sln và các file csproj để restore dependencies (tận dụng layer cache)
COPY ["backend/ClinicAppointment.sln", "backend/"]
COPY ["backend/src/ClinicAppointment.Domain/ClinicAppointment.Domain.csproj", "backend/src/ClinicAppointment.Domain/"]
COPY ["backend/src/ClinicAppointment.Application/ClinicAppointment.Application.csproj", "backend/src/ClinicAppointment.Application/"]
COPY ["backend/src/ClinicAppointment.Infrastructure/ClinicAppointment.Infrastructure.csproj", "backend/src/ClinicAppointment.Infrastructure/"]
COPY ["backend/src/ClinicAppointment.Api/ClinicAppointment.Api.csproj", "backend/src/ClinicAppointment.Api/"]
COPY ["backend/tests/ClinicAppointment.Domain.Tests/ClinicAppointment.Domain.Tests.csproj", "backend/tests/ClinicAppointment.Domain.Tests/"]

RUN dotnet restore "backend/ClinicAppointment.sln"

# Copy toàn bộ mã nguồn backend
COPY backend/ backend/

WORKDIR "/src/backend/src/ClinicAppointment.Api"
RUN dotnet build "ClinicAppointment.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ClinicAppointment.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# STAGE 2: Runtime ASP.NET Core Image (Non-root user for security)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Thiết lập biến môi trường chuẩn production
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

# Expose cổng nội bộ của Kestrel
EXPOSE 8080

COPY --from=publish /app/publish .

# Chạy với quyền non-root app user tích hợp sẵn của .NET 8
USER $APP_UID

ENTRYPOINT ["dotnet", "ClinicAppointment.Api.dll"]
