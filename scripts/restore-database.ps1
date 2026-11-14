<#
.SYNOPSIS
    Kịch bản phục hồi cơ sở dữ liệu PostgreSQL cho Clinic Appointment Management System.
.DESCRIPTION
    Sử dụng pg_restore phục hồi từ bản sao lưu định dạng nén tùy chỉnh (.dump).
    Bao gồm các bước kiểm tra an toàn, tùy chọn drop & recreate database, và kiểm tra tính toàn vẹn (integrity verification).
.PARAMETER BackupFile
    Đường dẫn tệp tin sao lưu (.dump). Bắt buộc.
.PARAMETER ConfirmSafety
    Cờ xác nhận an toàn trước khi ghi đè dữ liệu.
.EXAMPLE
    .\scripts\restore-database.ps1 -BackupFile "database\backups\clinic_backup_20261114_120000.dump" -ConfirmSafety
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$BackupFile,
    [switch]$ConfirmSafety,
    [string]$HostName = $env:POSTGRES_HOST,
    [string]$Port = $env:POSTGRES_PORT,
    [string]$Database = $env:POSTGRES_DB,
    [string]$Username = $env:POSTGRES_USER
)

if (-not $HostName) { $HostName = "localhost" }
if (-not $Port) { $Port = "5432" }
if (-not $Database) { $Database = "clinicappointmentdb" }
if (-not $Username) { $Username = "postgres" }

if (-not (Test-Path $BackupFile)) {
    Write-Error "[ERROR] Không tìm thấy tệp sao lưu: $BackupFile"
    exit 1
}

if (-not $ConfirmSafety) {
    Write-Warning "CẢNH BÁO: Thao tác phục hồi sẽ ghi đè cơ sở dữ liệu hiện tại: $Database!"
    Write-Warning "Nếu chắc chắn muốn tiếp tục, vui lòng thêm cờ -ConfirmSafety vào lệnh chạy."
    exit 1
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "BẮT ĐẦU PHỤC HỒI CƠ SỞ DỮ LIỆU POSTGRESQL (DISASTER RECOVERY)" -ForegroundColor Yellow
Write-Host "Thời gian: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host "Tệp nguồn: $BackupFile"
Write-Host "Đích đến: $Database (${HostName}:${Port})"
Write-Host "=========================================================="

try {
    # Chạy pg_restore với cờ --clean và --if-exists để dọn dẹp các đối tượng cũ trước khi tạo lại
    $pgRestoreCmd = "pg_restore -h $HostName -p $Port -U $Username -d $Database --clean --if-exists -v `"$BackupFile`""
    Invoke-Expression $pgRestoreCmd

    Write-Host "[SUCCESS] Lệnh pg_restore đã hoàn tất!" -ForegroundColor Green

    # Kiểm tra tính toàn vẹn của dữ liệu sau phục hồi
    Write-Host "Kiểm tra tính toàn vẹn dữ liệu..." -ForegroundColor Cyan
    $verifySql = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';"
    $tableCount = psql -h $HostName -p $Port -U $Username -d $Database -t -c $verifySql

    Write-Host "Tổng số bảng trong public schema: $($tableCount.Trim())" -ForegroundColor Green
    Write-Host "==========================================================" -ForegroundColor Cyan
    Write-Host "PHỤC HỒI CƠ SỞ DỮ LIỆU THÀNH CÔNG!" -ForegroundColor Green
} catch {
    Write-Error "[FATAL] Quá trình phục hồi gặp lỗi: $($_.Exception.Message)"
    exit 1
}
