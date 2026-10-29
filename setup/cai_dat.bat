@echo off
setlocal
cd /d "%~dp0"
title Trinh Cai Dat 1-Click LAN Control Multi-VLAN Suite (Agent / Server / Full)

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [THONG BAO] Dang yeu cau quyen Administrator de cai dat LAN Control...
    powershell -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

if exist "LanControlSetup.exe" (
    start "" "%~dp0LanControlSetup.exe" %*
) else if exist "Setup.exe" (
    start "" "%~dp0Setup.exe" %*
) else (
    echo [LOI] Khong tim thay tep LanControlSetup.exe trong thu muc setup!
    pause
)
