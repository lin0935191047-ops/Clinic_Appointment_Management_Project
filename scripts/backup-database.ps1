<#
.SYNOPSIS
    Kịch bản tự động sao lưu cơ sở dữ liệu PostgreSQL cho Clinic Appointment Management System.
.DESCRIPTION
    Sử dụng pg_dump tạo bản sao lưu định dạng nén tùy chỉnh (.dump) hoặc nén gzip (.sql.gz).
    Tự động gắn dấu thời gian UTC, kiểm tra mã lỗi và dọn dẹp các bản sao lưu cũ theo chính sách lưu trữ (retention policy).
.PARAMETER BackupDir
    Thư mục lưu trữ bản sao lưu. Mặc định là ./database/backups.
.PARAMETER RetentionDays
    Số ngày lưu trữ bản sao lưu trước khi tự động dọn dẹp. Mặc định: 14 ngày.
.EXAMPLE
    .\scripts\backup-database.ps1
    .\scripts\backup-database.ps1 -BackupDir "C:\backups\clinic" -RetentionDays 30
#>

param(
    [string]$BackupDir = "database/backups",
    [int]$RetentionDays = 14,
    [string]$HostName = $env:POSTGRES_HOST,
    [string]$Port = $env:POSTGRES_PORT,
    [string]$Database = $env:POSTGRES_DB,
    [string]$Username = $env:POSTGRES_USER
)

if (-not $HostName) { $HostName = "localhost" }
if (-not $Port) { $Port = "5432" }
if (-not $Database) { $Database = "clinicappointmentdb" }
if (-not $Username) { $Username = "postgres" }

# Tạo thư mục lưu backup nếu chưa tồn tại
if (-not (Test-Path $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $BackupDir "clinic_backup_${timestamp}.dump"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "BẮT ĐẦU SAO LƯU CƠ SỞ DỮ LIỆU POSTGRESQL (CLINIC SYSTEM)" -ForegroundColor Cyan
Write-Host "Thời gian: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host "Database: $Database | Host: ${HostName}:${Port} | User: $Username"
Write-Host "File đích: $backupFile"
Write-Host "=========================================================="

try {
    # Chạy pg_dump với định dạng Custom Format (-Fc) có nén và hỗ trợ pg_restore song song
    $pgDumpCmd = "pg_dump -h $HostName -p $Port -U $Username -Fc -b -v -f `"$backupFile`" $Database"
    Invoke-Expression $pgDumpCmd

    if ($LASTEXITCODE -eq 0 -and (Test-Path $backupFile)) {
        $fileSizeMb = [math]::Round((Get-Item $backupFile).Length / 1MB, 2)
        Write-Host "[SUCCESS] Sao lưu thành công! Kích thước file: $fileSizeMb MB" -ForegroundColor Green
    } else {
        Write-Error "[ERROR] pg_dump thoát với mã lỗi: $LASTEXITCODE"
        exit 1
    }

    # Áp dụng chính sách lưu trữ: Xóa các file backup cũ hơn $RetentionDays ngày
    Write-Host "Kiểm tra và dọn dẹp các file backup cũ hơn $RetentionDays ngày..." -ForegroundColor Gray
    $thresholdDate = (Get-Date).AddDays(-$RetentionDays)
    Get-ChildItem -Path $BackupDir -Filter "clinic_backup_*.dump" | Where-Object { $_.LastWriteTime -lt $thresholdDate } | ForEach-Object {
        Write-Host "Xóa file backup hết hạn: $($_.Name)" -ForegroundColor Yellow
        Remove-Item $_.FullName -Force
    }

    Write-Host "==========================================================" -ForegroundColor Cyan
    Write-Host "QUY TRÌNH SAO LƯU HOÀN TẤT THÀNH CÔNG!" -ForegroundColor Green
} catch {
    Write-Error "[FATAL] Quá trình sao lưu gặp ngoại lệ: $($_.Exception.Message)"
    exit 1
}
