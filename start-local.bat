@echo off
title Clinic Appointment Management - Local
cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -File "%~dp0scripts\start-local.ps1"
pause
