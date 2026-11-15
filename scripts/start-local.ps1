# Clinic Appointment Management - Local Launcher
$PSScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $ProjectRoot

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   CLINIC APPOINTMENT MANAGEMENT - LOCAL LAUNCHER         " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Backend
Write-Host "`n[1/2] Kiem tra Backend API (port 5000)..." -ForegroundColor Yellow
$backendOk = $false
try {
    $res = Invoke-WebRequest -Uri "http://localhost:5000/health" -UseBasicParsing -TimeoutSec 2
    if ($res.StatusCode -eq 200) { $backendOk = $true }
} catch {}

if ($backendOk) {
    Write-Host "  -> Backend API dang hoat dong tot (200 OK)." -ForegroundColor Green
} else {
    Write-Host "  -> Dang khoi dong Backend API..." -ForegroundColor Cyan
    Start-Process -FilePath "dotnet" -ArgumentList "run --project backend/src/ClinicAppointment.Api --launch-profile http" -WorkingDirectory $ProjectRoot -WindowStyle Minimized
    
    $attempts = 0
    while ($attempts -lt 25) {
        Start-Sleep -Seconds 1
        $attempts++
        try {
            $test = Invoke-WebRequest -Uri "http://localhost:5000/health" -UseBasicParsing -TimeoutSec 1
            if ($test.StatusCode -eq 200) {
                Write-Host "  -> Backend API da khoi dong thanh cong!" -ForegroundColor Green
                break
            }
        } catch {}
    }
}

# 2. Frontend
Write-Host "`n[2/2] Kiem tra Frontend Vite (port 5173)..." -ForegroundColor Yellow
$frontendOk = $false
try {
    $res = Invoke-WebRequest -Uri "http://localhost:5173" -UseBasicParsing -TimeoutSec 2
    if ($res.StatusCode -eq 200) { $frontendOk = $true }
} catch {}

if ($frontendOk) {
    Write-Host "  -> Frontend dang hoat dong tot (200 OK)." -ForegroundColor Green
} else {
    Write-Host "  -> Dang khoi dong Frontend..." -ForegroundColor Cyan
    $npmCmd = "npm.cmd"
    if (-not (Get-Command "npm.cmd" -ErrorAction SilentlyContinue)) { $npmCmd = "npm" }
    Start-Process -FilePath $npmCmd -ArgumentList "run dev" -WorkingDirectory (Join-Path $ProjectRoot "frontend") -WindowStyle Minimized
    
    $attempts = 0
    while ($attempts -lt 20) {
        Start-Sleep -Seconds 1
        $attempts++
        try {
            $test = Invoke-WebRequest -Uri "http://localhost:5173" -UseBasicParsing -TimeoutSec 1
            if ($test.StatusCode -eq 200) {
                Write-Host "  -> Frontend da khoi dong thanh cong!" -ForegroundColor Green
                break
            }
        } catch {}
    }
}

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "   HE THONG PHONG KHAM DA SAN SANG!                       " -ForegroundColor Green
Write-Host "   Dia chi truy cap: http://localhost:5173                " -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Green
Write-Host "`nDang tu dong mo trinh duyet..." -ForegroundColor Cyan

Start-Process "http://localhost:5173"
