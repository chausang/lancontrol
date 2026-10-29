@echo off
title LAN Control Client Agent - VLAN 10 (Tech)
echo ===================================================================
echo     STARTING LAN CONTROL CLIENT AGENT (VLAN 10 - TECH DEPT)
echo ===================================================================
echo.
cd /d "%~dp0"
if not exist "bin\LanControl.Agent.exe" (
    echo [INFO] Binaries not found. Running build.bat first...
    call build.bat
)
start "" "bin\LanControl.Agent.exe" 127.0.0.1 8443 "VLAN 10 - Tech"
echo [OK] LAN Control Agent started in System Tray (VLAN 10 - Tech).
echo      Right-click the blue tray icon 'N' to check status.
ping 127.0.0.1 -n 3 >nul
