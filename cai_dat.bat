@echo off
setlocal
cd /d "%~dp0"
title Trinh Cai Dat 1-Click LAN Control Multi-VLAN Suite (Agent / Server / Full)

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [THONG BAO] Dang yeu cau quyen Administrator de mo Trinh Cai Dat 1-Click LAN Control...
    powershell -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

if exist "LanControlSetup.exe" (
    start "" "%~dp0LanControlSetup.exe" %*
    exit /b 0
)

if exist "setup\LanControlSetup.exe" (
    start "" "%~dp0setup\LanControlSetup.exe" %*
    exit /b 0
)

if exist "bin\LanControlSetup.exe" (
    start "" "%~dp0bin\LanControlSetup.exe" %*
    exit /b 0
)

echo [THONG BAO] Dang bien dich Trinh Cai Dat 1-Click lan dau...
call build.bat

if exist "LanControlSetup.exe" (
    start "" "%~dp0LanControlSetup.exe" %*
) else if exist "setup\LanControlSetup.exe" (
    start "" "%~dp0setup\LanControlSetup.exe" %*
) else (
    echo [LOI] Khong tim thay LanControlSetup.exe! Vui long kiem tra lai.
    pause
)

