@echo off
title Cap Nhat Nhanh LAN Control Multi-VLAN Suite
cd /d "%~dp0"

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [THONG BAO] Dang yeu cau quyen Administrator de cap nhat file...
    powershell -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

echo ===================================================================
echo     TRINH CAP NHAT NHANH LAN CONTROL (HOT-UPDATE KHONG MAT CAU HINH)
echo ===================================================================
echo.

set TARGET_DIR=C:\Program Files\lancontrol
if not exist "%TARGET_DIR%" (
    if exist "C:\lancontrol" (
        set TARGET_DIR=C:\lancontrol
    ) else (
        echo [THONG BAO] Khong tim thay thu muc cai dat mac dinh C:\Program Files\lancontrol.
        echo Dang mo Trinh Cai Dat Wizard de ban chon thu muc...
        start "" "LanControlSetup.exe"
        exit /b 0
    )
)

echo [1/4] Dang dung cac tien trinh LAN Control dang chay...
taskkill /f /im LanControl.Agent.exe >nul 2>&1
taskkill /f /im LanControl.Control.exe >nul 2>&1
taskkill /f /im LanControl.Gateway.exe >nul 2>&1
timeout /t 1 /nobreak >nul

echo [2/4] Dang sao chep tep cap nhat moi vao: %TARGET_DIR%...
if exist "LanControl.Core.dll" copy /y "LanControl.Core.dll" "%TARGET_DIR%\" >nul
if exist "LanControl.Agent.exe" copy /y "LanControl.Agent.exe" "%TARGET_DIR%\" >nul
if exist "LanControl.Control.exe" copy /y "LanControl.Control.exe" "%TARGET_DIR%\" >nul
if exist "LanControl.Gateway.exe" copy /y "LanControl.Gateway.exe" "%TARGET_DIR%\" >nul
if exist "icon.ico" copy /y "icon.ico" "%TARGET_DIR%\" >nul
if exist "about.md" copy /y "about.md" "%TARGET_DIR%\" >nul
if exist "HUONG_DAN_UPDATE.md" copy /y "HUONG_DAN_UPDATE.md" "%TARGET_DIR%\" >nul

echo [3/4] Dang cap nhat cau hinh ho tro UAC Admin...
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" /v PromptOnSecureDesktop /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" /v EnableUIPI /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" /v ConsentPromptBehaviorAdmin /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" /v ConsentPromptBehaviorUser /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" /v EnableSecureUIAPaths /t REG_DWORD /d 0 /f >nul 2>&1

echo [4/4] Dang khoi dong lai dich vu...
if exist "%TARGET_DIR%\LanControl.Agent.exe" (
    start "" "%TARGET_DIR%\LanControl.Agent.exe"
    echo   - Da khoi dong lai LAN Control Agent.
)

echo.
echo ===================================================================
echo [THANH CONG] DA CAP NHAT PHIEN BAN MOI NHAT THANH CONG!
echo Toan bo file cau hinh (agent_config.json) duoc giu nguyen ven 100%%.
echo ===================================================================
echo.
timeout /t 3 >nul
