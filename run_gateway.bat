@echo off
title LAN Control Gateway Server - Central Relay Hub
echo ===================================================================
echo           STARTING LAN CONTROL CENTRAL GATEWAY (PORT 8443)
echo ===================================================================
echo.
cd /d "%~dp0"
if not exist "bin\LanControl.Gateway.exe" (
    echo [INFO] Binaries not found. Running build.bat first...
    call build.bat
)
"bin\LanControl.Gateway.exe" 8443
pause
