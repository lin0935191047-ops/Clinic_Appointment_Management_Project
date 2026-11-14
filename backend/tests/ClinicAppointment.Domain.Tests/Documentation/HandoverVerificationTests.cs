using System;
using System.IO;
using System.Text.Json;
using Xunit;

namespace ClinicAppointment.Domain.Tests.Documentation;

public class HandoverVerificationTests
{
    private static string FindProjectRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "ClinicAppointment.sln")) ||
                File.Exists(Path.Combine(current, "AGENTS.md")) ||
                Directory.Exists(Path.Combine(current, "docs")))
            {
                if (File.Exists(Path.Combine(current, "AGENTS.md")))
                {
                    return current;
                }
                var parent = Directory.GetParent(current)?.FullName;
                if (parent != null && File.Exists(Path.Combine(parent, "AGENTS.md")))
                {
                    return parent;
                }
            }
            var next = Directory.GetParent(current)?.FullName;
            if (next == current) break;
            current = next;
        }

        var candidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        if (File.Exists(Path.Combine(candidate, "AGENTS.md")))
        {
            return candidate;
        }

        return @"d:\Clinic_Appointment_Management_Project";
    }

    [Fact]
    public void ReadmeFile_ExistsAndContainsVerifiedRunCommands()
    {
        var projectRoot = FindProjectRoot();
        var readmePath = Path.Combine(projectRoot, "README.md");

        Assert.True(File.Exists(readmePath), $"README.md must exist at {readmePath}");

        var content = File.ReadAllText(readmePath);

        // Verify key sections
        Assert.Contains("Clinic Appointment Management System", content);
        Assert.Contains("Clean Architecture", content);
        Assert.Contains("PostgreSQL", content);

        // Verify run commands for local development
        Assert.Contains("dotnet ef database update", content);
        Assert.Contains("dotnet run", content);
        Assert.Contains("npm run dev", content);
        Assert.Contains("http://localhost:5000", content);
        Assert.Contains("http://localhost:5173", content);

        // Verify testing commands
        Assert.Contains("dotnet test", content);
        Assert.Contains("npm --prefix", content);

        // Verify production deployment commands
        Assert.Contains("docker-compose.prod.yml", content);
        Assert.Contains("docker compose", content);

        // Verify demo accounts
        Assert.Contains("admin@clinic.com", content);
        Assert.Contains("receptionist@clinic.com", content);
        Assert.Contains("doctor.an@clinic.com", content);

        // Verify known limitations / TBD documented
        Assert.Contains("SMS Gateway Provider (TBD)", content);
        Assert.Contains("Cổng thanh toán trực tuyến (TBD)", content);
        Assert.Contains("Email SMTP Provider (TBD)", content);
    }

    [Fact]
    public void HandoverDocument_ExistsAndCoversAll55FeaturesAndEvidence()
    {
        var projectRoot = FindProjectRoot();
        var handoverPath = Path.Combine(projectRoot, "docs", "HANDOVER.md");

        Assert.True(File.Exists(handoverPath), $"docs/HANDOVER.md must exist at {handoverPath}");

        var content = File.ReadAllText(handoverPath);

        // Verify handover report structure
        Assert.Contains("BÁO CÁO BÀN GIAO VÀ NGHIỆM THU DỰ ÁN", content);
        Assert.Contains("55/55 Features Completed", content);
        Assert.Contains("FEAT-001", content);
        Assert.Contains("FEAT-022", content);
        Assert.Contains("FEAT-055", content);

        // Verify test evidence summaries
        Assert.Contains("514", content);
        Assert.Contains("Backend Test Suites", content);
        Assert.Contains("Frontend Test Suites", content);

        // Verify known limitations and TBD documented
        Assert.Contains("CÁC HẠN CHẾ ĐÃ BIẾT VÀ HẠNG MỤC TBD", content);
        Assert.Contains("SMS Gateway Provider (TBD)", content);
        Assert.Contains("Online Payment Gateway Provider (TBD)", content);
        Assert.Contains("Email SMTP Provider (TBD)", content);

        // Verify operational guides
        Assert.Contains("HƯỚNG DẪN BÀN GIAO VẬN HÀNH VÀ LỆNH KHỞI CHẠY ĐÃ XÁC MINH", content);
        Assert.Contains("backup-database", content);
        Assert.Contains("restore-database", content);
    }

    [Fact]
    public void FeatureListJson_ContainsExactly55FeaturesAndValidStructure()
    {
        var projectRoot = FindProjectRoot();
        var featureListPath = Path.Combine(projectRoot, "planning", "feature_list.json");

        Assert.True(File.Exists(featureListPath), $"planning/feature_list.json must exist at {featureListPath}");

        var json = File.ReadAllText(featureListPath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("features", out var featuresElement));
        Assert.Equal(55, featuresElement.GetArrayLength());

        var first = featuresElement[0];
        Assert.Equal("FEAT-001", first.GetProperty("id").GetString());

        var last = featuresElement[54];
        Assert.Equal("FEAT-055", last.GetProperty("id").GetString());
    }
}
