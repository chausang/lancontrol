@echo off
title LAN Control Client Agent - VLAN 20 (Accounting)
echo ===================================================================
echo   STARTING LAN CONTROL CLIENT AGENT (VLAN 20 - ACCOUNTING DEPT)
echo ===================================================================
echo.
cd /d "%~dp0"
if not exist "bin\LanControl.Agent.exe" (
    echo [INFO] Binaries not found. Running build.bat first...
    call build.bat
)
start "" "bin\LanControl.Agent.exe" 127.0.0.1 8443 "VLAN 20 - Accounting"
echo [OK] LAN Control Agent started in System Tray (VLAN 20 - Accounting).
ping 127.0.0.1 -n 3 >nul
