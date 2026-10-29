@echo off
title LAN Control Master Control Console
echo ===================================================================
echo             STARTING LAN CONTROL MASTER CONTROL CONSOLE
echo ===================================================================
echo.
cd /d "%~dp0"
if not exist "bin\LanControl.Control.exe" (
    echo [INFO] Binaries not found. Running build.bat first...
    call build.bat
)
start "" "bin\LanControl.Control.exe"
echo [OK] LAN Control Master Control Console launched.
ping 127.0.0.1 -n 3 >nul
