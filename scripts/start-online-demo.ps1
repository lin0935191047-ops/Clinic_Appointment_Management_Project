# Script khởi chạy Demo Online qua Cloudflare Tunnel
param (
    [int]$Port = 5173
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   CLINIC APPOINTMENT MANAGEMENT — ONLINE DEMO LAUNCHER   " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Kiểm tra Backend API
Write-Host "`n[1/3] Kiem tra Backend API (port 5000)..." -ForegroundColor Yellow
try {
    $backendRes = Invoke-WebRequest -Uri "http://localhost:5000/health" -UseBasicParsing -TimeoutSec 3
    if ($backendRes.StatusCode -eq 200) {
        Write-Host "  -> Backend API dang hoat dong tot (200 OK)." -ForegroundColor Green
    }
} catch {
    Write-Host "  [CANH BAO] Backend API chua chay tai http://localhost:5000." -ForegroundColor Red
    Write-Host "  Vui long chay lenh sau o cua so khac:" -ForegroundColor Yellow
    Write-Host "  dotnet run --project backend/src/ClinicAppointment.Api --launch-profile http" -ForegroundColor White
}

# 2. Kiểm tra Frontend Vite
Write-Host "`n[2/3] Kiem tra Frontend Vite (port 5173)..." -ForegroundColor Yellow
try {
    $frontendRes = Invoke-WebRequest -Uri "http://localhost:5173" -UseBasicParsing -TimeoutSec 3
    if ($frontendRes.StatusCode -eq 200) {
        Write-Host "  -> Frontend dang hoat dong tot (200 OK)." -ForegroundColor Green
    }
} catch {
    Write-Host "  [CANH BAO] Frontend chua chay tai http://localhost:5173." -ForegroundColor Red
    Write-Host "  Vui long chay lenh sau o cua so khac:" -ForegroundColor Yellow
    Write-Host "  cd frontend; npm run dev" -ForegroundColor White
}

# 3. Kiem tra cloudflared.exe
Write-Host "`n[3/3] Khoi dong Cloudflare Tunnel..." -ForegroundColor Yellow
if (-not (Test-Path "cloudflared.exe")) {
    Write-Host "  Dang tai cloudflared.exe..." -ForegroundColor Cyan
    Invoke-WebRequest -Uri "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe" -OutFile "cloudflared.exe"
}

Write-Host "  Dang mo duong ham HTTPS cong khai..." -ForegroundColor Green
Write-Host "  (Tim dong co chua link: https://xxxx.trycloudflare.com ben duoi)`n" -ForegroundColor Cyan

& ".\cloudflared.exe" tunnel --url "http://localhost:$Port"
