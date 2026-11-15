@echo off
title Clinic Appointment Management - Online Demo
cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -File "%~dp0scripts\start-online-demo.ps1"
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Da xay ra loi khi khoi chay. Nhan phim bat ky de thoat...
    pause >nul
)
