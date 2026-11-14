using System.IO;
using FluentAssertions;
using Xunit;

namespace ClinicAppointment.Domain.Tests.Operations;

/// <summary>
/// Bộ kiểm thử tự động xác nhận cấu hình triển khai, môi trường và runbook vận hành (FEAT-054)
/// Kiểm chứng 3 tiêu chí nghiệm thu cốt lõi:
/// 1. No secrets in repo
/// 2. HTTPS in target env
/// 3. Backup/restore guide
/// </summary>
public class DeploymentAndEnvironmentConfigurationTests
{
    private readonly string _rootDir;

    public DeploymentAndEnvironmentConfigurationTests()
    {
        // Định vị thư mục gốc của dự án từ thư mục test bin
        var currentDir = Directory.GetCurrentDirectory();
        var dir = new DirectoryInfo(currentDir);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "docker-compose.yml")))
        {
            dir = dir.Parent;
        }

        _rootDir = dir?.FullName ?? throw new InvalidOperationException("Không thể định vị thư mục gốc của dự án.");
    }

    #region 1. Acceptance Criteria: No Secrets in Repo

    [Fact]
    public void GitIgnore_ProperlyConfigured_ExcludesAllSensitiveFiles()
    {
        var gitignorePath = Path.Combine(_rootDir, ".gitignore");
        File.Exists(gitignorePath).Should().BeTrue("File .gitignore bắt buộc phải tồn tại ở thư mục gốc");

        var content = File.ReadAllText(gitignorePath);

        // Bắt buộc loại trừ các tệp nhạy cảm
        content.Should().Contain(".env", "Phải loại trừ tệp biến môi trường .env");
        content.Should().Contain("appsettings.Production.json", "Phải loại trừ appsettings.Production.json");
        content.Should().Contain("*.dump", "Phải loại trừ tệp sao lưu database *.dump");
        content.Should().Contain("*.backup", "Phải loại trừ tệp sao lưu database *.backup");
        content.Should().Contain("*.sql.gz", "Phải loại trừ tệp nén SQL *.sql.gz");
    }

    [Fact]
    public void EnvironmentTemplates_ExistAndDoNotContainRealSecrets()
    {
        var rootEnvExample = Path.Combine(_rootDir, ".env.example");
        var frontendEnvExample = Path.Combine(_rootDir, "frontend", ".env.example");
        var backendAppsettingsExample = Path.Combine(
            _rootDir, "backend", "src", "ClinicAppointment.Api", "appsettings.Production.json.example");

        File.Exists(rootEnvExample).Should().BeTrue(".env.example ở thư mục gốc phải tồn tại");
        File.Exists(frontendEnvExample).Should().BeTrue("frontend/.env.example phải tồn tại");
        File.Exists(backendAppsettingsExample).Should().BeTrue("appsettings.Production.json.example phải tồn tại");

        var rootContent = File.ReadAllText(rootEnvExample);
        var backendContent = File.ReadAllText(backendAppsettingsExample);

        // Không chứa mật khẩu thật
        rootContent.Should().Contain("your_postgres_password_here");
        rootContent.Should().NotContain("Postgres@123", "Template .env.example không được chứa mật khẩu thật");

        backendContent.Should().Contain("YOUR_SECURE_POSTGRES_PASSWORD_HERE");
        backendContent.Should().NotContain("Postgres@123", "appsettings.Production.json.example không được chứa mật khẩu thật");
    }

    [Fact]
    public void DockerIgnoreFiles_ExistInBothBackendAndFrontend()
    {
        var backendDockerIgnore = Path.Combine(_rootDir, "backend", ".dockerignore");
        var frontendDockerIgnore = Path.Combine(_rootDir, "frontend", ".dockerignore");

        File.Exists(backendDockerIgnore).Should().BeTrue("backend/.dockerignore phải tồn tại để loại trừ secrets và build artifacts");
        File.Exists(frontendDockerIgnore).Should().BeTrue("frontend/.dockerignore phải tồn tại để loại trừ node_modules và .env");

        File.ReadAllText(backendDockerIgnore).Should().Contain(".env");
        File.ReadAllText(backendDockerIgnore).Should().Contain("bin");
        File.ReadAllText(frontendDockerIgnore).Should().Contain(".env");
        File.ReadAllText(frontendDockerIgnore).Should().Contain("node_modules");
    }

    #endregion

    #region 2. Acceptance Criteria: HTTPS in Target Env

    [Fact]
    public void NginxConfiguration_EnforcesHttpToHttps301Redirect()
    {
        var nginxConfPath = Path.Combine(_rootDir, "deployment", "nginx", "conf.d", "clinic.conf");
        File.Exists(nginxConfPath).Should().BeTrue("File cấu hình Nginx clinic.conf phải tồn tại");

        var content = File.ReadAllText(nginxConfPath);

        // Kiểm tra chuyển hướng 301 sang HTTPS trên cổng 80
        content.Should().Contain("listen 80;");
        content.Should().Contain("return 301 https://$host$request_uri;");
    }

    [Fact]
    public void NginxConfiguration_EnforcesHstsAndSecurityHeaders()
    {
        var nginxConfPath = Path.Combine(_rootDir, "deployment", "nginx", "conf.d", "clinic.conf");
        var content = File.ReadAllText(nginxConfPath);

        // Kiểm tra HSTS
        content.Should().Contain("Strict-Transport-Security");
        content.Should().Contain("max-age=31536000", "HSTS phải có thời hạn tối thiểu 1 năm");

        // Kiểm tra Security headers
        content.Should().Contain("X-Frame-Options");
        content.Should().Contain("X-Content-Type-Options");
        content.Should().Contain("X-Correlation-ID", "Nginx phải forward header X-Correlation-ID cho backend");
        content.Should().Contain("ssl_protocols TLSv1.2 TLSv1.3;", "Chỉ cho phép các giao thức TLS an toàn");
    }

    [Fact]
    public void DockerComposeProd_ProperlyConfiguresServicesAndHttpsPorts()
    {
        var composeProdPath = Path.Combine(_rootDir, "docker-compose.prod.yml");
        File.Exists(composeProdPath).Should().BeTrue("docker-compose.prod.yml phải tồn tại");

        var content = File.ReadAllText(composeProdPath);

        // Phải có 4 dịch vụ cốt lõi
        content.Should().Contain("postgres:");
        content.Should().Contain("backend:");
        content.Should().Contain("frontend:");
        content.Should().Contain("nginx:");

        // Nginx expose ports 80 và 443
        content.Should().Contain("\"80:80\"");
        content.Should().Contain("\"443:443\"");

        // Postgres không expose port ra ngoài host trong production
        content.Should().NotContain("\"5432:5432\"", "Trong production, PostgreSQL không được expose port trực tiếp ra host/public internet");
    }

    [Fact]
    public void Dockerfiles_ExistWithMultiStageBuilds()
    {
        var backendDockerfile = Path.Combine(_rootDir, "backend", "Dockerfile");
        var frontendDockerfile = Path.Combine(_rootDir, "frontend", "Dockerfile");

        File.Exists(backendDockerfile).Should().BeTrue("backend/Dockerfile phải tồn tại");
        File.Exists(frontendDockerfile).Should().BeTrue("frontend/Dockerfile phải tồn tại");

        var backendContent = File.ReadAllText(backendDockerfile);
        backendContent.Should().Contain("FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build");
        backendContent.Should().Contain("USER $APP_UID", "Backend Docker container phải chạy bằng non-root user");

        var frontendContent = File.ReadAllText(frontendDockerfile);
        frontendContent.Should().Contain("FROM node:20-alpine AS build");
        frontendContent.Should().Contain("FROM nginx:1.27-alpine AS final");
    }

    #endregion

    #region 3. Acceptance Criteria: Backup & Restore Guide

    [Fact]
    public void DeploymentRunbook_ExistsAndCoversAllRequiredSections()
    {
        var runbookPath = Path.Combine(_rootDir, "docs", "DEPLOYMENT_RUNBOOK.md");
        File.Exists(runbookPath).Should().BeTrue("docs/DEPLOYMENT_RUNBOOK.md phải tồn tại");

        var content = File.ReadAllText(runbookPath);

        // Bao phủ toàn diện các phần nghiệp vụ
        content.Should().Contain("No Secrets in Repo");
        content.Should().Contain("301 Moved Permanently");
        content.Should().Contain("HSTS");
        content.Should().Contain("pg_dump", "Phải hướng dẫn lệnh sao lưu pg_dump");
        content.Should().Contain("pg_restore", "Phải hướng dẫn lệnh phục hồi pg_restore");
        content.Should().Contain("ex_appointments_doctor_no_overlap", "Phải có bước kiểm tra ràng buộc GiST sau khi restore");
    }

    [Fact]
    public void BackupAndRestoreScripts_ExistForBothWindowsAndLinux()
    {
        var backupPs1 = Path.Combine(_rootDir, "scripts", "backup-database.ps1");
        var backupSh = Path.Combine(_rootDir, "scripts", "backup-database.sh");
        var restorePs1 = Path.Combine(_rootDir, "scripts", "restore-database.ps1");
        var restoreSh = Path.Combine(_rootDir, "scripts", "restore-database.sh");

        File.Exists(backupPs1).Should().BeTrue("scripts/backup-database.ps1 phải tồn tại");
        File.Exists(backupSh).Should().BeTrue("scripts/backup-database.sh phải tồn tại");
        File.Exists(restorePs1).Should().BeTrue("scripts/restore-database.ps1 phải tồn tại");
        File.Exists(restoreSh).Should().BeTrue("scripts/restore-database.sh phải tồn tại");

        File.ReadAllText(backupPs1).Should().Contain("pg_dump");
        File.ReadAllText(backupSh).Should().Contain("pg_dump");
        File.ReadAllText(restorePs1).Should().Contain("pg_restore");
        File.ReadAllText(restoreSh).Should().Contain("pg_restore");
    }

    #endregion
}
