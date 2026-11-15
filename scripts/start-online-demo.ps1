# Clinic Appointment Management - Online Demo Launcher
param (
    [int]$FrontendPort = 5173,
    [int]$BackendPort = 5000
)

$PSScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $ProjectRoot

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   CLINIC APPOINTMENT MANAGEMENT - ONLINE DEMO LAUNCHER   " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Kiem tra va khoi dong Backend API neu chua chay
Write-Host "`n[1/3] Kiem tra Backend API (port $BackendPort)..." -ForegroundColor Yellow
$backendRunning = $false
try {
    $res = Invoke-WebRequest -Uri "http://localhost:$BackendPort/health" -UseBasicParsing -TimeoutSec 2
    if ($res.StatusCode -eq 200) { $backendRunning = $true }
} catch {}

if ($backendRunning) {
    Write-Host "  -> Backend API dang hoat dong tot (200 OK)." -ForegroundColor Green
} else {
    Write-Host "  -> Backend chua chay. Dang khoi dong Backend API..." -ForegroundColor Cyan
    Start-Process -FilePath "dotnet" -ArgumentList "run --project backend/src/ClinicAppointment.Api --launch-profile http" -WorkingDirectory $ProjectRoot -WindowStyle Minimized
    
    $attempts = 0
    while ($attempts -lt 25) {
        Start-Sleep -Seconds 1
        $attempts++
        try {
            $test = Invoke-WebRequest -Uri "http://localhost:$BackendPort/health" -UseBasicParsing -TimeoutSec 1
            if ($test.StatusCode -eq 200) {
                Write-Host "  -> Backend API da khoi dong thanh cong!" -ForegroundColor Green
                $backendRunning = $true
                break
            }
        } catch {}
    }
}

# 2. Kiem tra va khoi dong Frontend Vite neu chua chay
Write-Host "`n[2/3] Kiem tra Frontend Vite (port $FrontendPort)..." -ForegroundColor Yellow
$frontendRunning = $false
try {
    $res = Invoke-WebRequest -Uri "http://localhost:$FrontendPort" -UseBasicParsing -TimeoutSec 2
    if ($res.StatusCode -eq 200) { $frontendRunning = $true }
} catch {}

if ($frontendRunning) {
    Write-Host "  -> Frontend dang hoat dong tot (200 OK)." -ForegroundColor Green
} else {
    Write-Host "  -> Frontend chua chay. Dang khoi dong Frontend..." -ForegroundColor Cyan
    $npmCmd = "npm.cmd"
    if (-not (Get-Command "npm.cmd" -ErrorAction SilentlyContinue)) { $npmCmd = "npm" }
    Start-Process -FilePath $npmCmd -ArgumentList "run dev" -WorkingDirectory (Join-Path $ProjectRoot "frontend") -WindowStyle Minimized
    
    $attempts = 0
    while ($attempts -lt 20) {
        Start-Sleep -Seconds 1
        $attempts++
        try {
            $test = Invoke-WebRequest -Uri "http://localhost:$FrontendPort" -UseBasicParsing -TimeoutSec 1
            if ($test.StatusCode -eq 200) {
                Write-Host "  -> Frontend da khoi dong thanh cong!" -ForegroundColor Green
                $frontendRunning = $true
                break
            }
        } catch {}
    }
}

# 3. Kiem tra cloudflared.exe
Write-Host "`n[3/3] Khoi dong Cloudflare Tunnel..." -ForegroundColor Yellow
$cloudflaredPath = Join-Path $ProjectRoot "cloudflared.exe"
if (-not (Test-Path $cloudflaredPath)) {
    Write-Host "  Dang tai cloudflared.exe..." -ForegroundColor Cyan
    Invoke-WebRequest -Uri "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe" -OutFile $cloudflaredPath
}

Write-Host "  Dang thiet lap duong ham HTTPS cong khai..." -ForegroundColor Green
Write-Host "  He thong se tu dong mo trinh duyet khi co link!`n" -ForegroundColor Cyan

$logFile = Join-Path $ProjectRoot "tunnel.log"
if (Test-Path $logFile) { Remove-Item $logFile -Force -ErrorAction SilentlyContinue }

$tunnelProc = Start-Process -FilePath $cloudflaredPath -ArgumentList "tunnel --url http://localhost:$FrontendPort --logfile `"$logFile`"" -PassThru -NoNewWindow

$tunnelUrl = ""
$attempts = 0
while ($attempts -lt 30 -and [string]::IsNullOrEmpty($tunnelUrl)) {
    Start-Sleep -Seconds 1
    $attempts++
    if (Test-Path $logFile) {
        $content = Get-Content $logFile -Raw -ErrorAction SilentlyContinue
        if ($content -match "https://[a-zA-Z0-9\-]+\.trycloudflare\.com") {
            $tunnelUrl = $matches[0]
            break
        }
    }
}

if (-not [string]::IsNullOrEmpty($tunnelUrl)) {
    Write-Host "==========================================================" -ForegroundColor Green
    Write-Host "   DEMO ONLINE DA SAN SANG TAI DUONG LINK DANG DUOI:      " -ForegroundColor Green
    Write-Host "   $tunnelUrl" -ForegroundColor Yellow
    Write-Host "==========================================================" -ForegroundColor Green
    Write-Host "`nDang tu dong mo trinh duyet..." -ForegroundColor Cyan
    Start-Process $tunnelUrl
} else {
    Write-Host "[THONG BAO] Khong the lay tu dong link trycloudflare. Vui long kiem tra file tunnel.log hoac ket noi Internet." -ForegroundColor Red
    Write-Host "Ban van co the truy cap truc tiep tai: http://localhost:$FrontendPort" -ForegroundColor Yellow
    Start-Process "http://localhost:$FrontendPort"
}

Write-Host "`n[GHI CHU] Giu nguyen cua so nay de duy tri link online. Dong cua so de tat demo." -ForegroundColor Gray
try {
    $tunnelProc.WaitForExit()
} catch {}
