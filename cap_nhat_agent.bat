@echo off
title Cap Nhat Nhanh LAN Control Agent
cd /d "%~dp0"

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [THONG BAO] Dang yeu cau quyen Administrator de cap nhat file...
    powershell -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

echo ===================================================================
echo        CAP NHAT NHANH LAN CONTROL AGENT VA HO TRO UAC ADMIN
echo ===================================================================
echo.

set TARGET_DIR=C:\Program Files\lancontrol
if not exist "%TARGET_DIR%" (
    if exist "C:\lancontrol" (
        set TARGET_DIR=C:\lancontrol
    ) else (
        echo [THONG BAO] Khong tim thay thu muc C:\Program Files\lancontrol.
        echo Dang chuyen sang mo trinh cai dat...
        if exist "setup\LanControlSetup.exe" start "" "setup\LanControlSetup.exe"
        exit /b 0
    )
)

echo [1/3] Dang dung LAN Control Agent...
taskkill /f /im LanControl.Agent.exe >nul 2>&1
timeout /t 1 /nobreak >nul

echo [2/3] Dang sao chep tep cap nhat moi vao: %TARGET_DIR%...
if exist "bin\LanControl.Core.dll" copy /y "bin\LanControl.Core.dll" "%TARGET_DIR%\" >nul
if exist "bin\LanControl.Agent.exe" copy /y "bin\LanControl.Agent.exe" "%TARGET_DIR%\" >nul
if exist "setup\LanControl.Core.dll" copy /y "setup\LanControl.Core.dll" "%TARGET_DIR%\" >nul
if exist "setup\LanControl.Agent.exe" copy /y "setup\LanControl.Agent.exe" "%TARGET_DIR%\" >nul
if exist "icon.ico" copy /y "icon.ico" "%TARGET_DIR%\" >nul

echo [3/3] Dang cap nhat cau hinh UAC va khoi dong lai...
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" /v PromptOnSecureDesktop /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" /v EnableUIPI /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" /v ConsentPromptBehaviorAdmin /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" /v ConsentPromptBehaviorUser /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" /v EnableSecureUIAPaths /t REG_DWORD /d 0 /f >nul 2>&1

start "" "%TARGET_DIR%\LanControl.Agent.exe"

echo.
echo ===================================================================
echo [THANH CONG] DA CAP NHAT PHIEN BAN LAN CONTROL AGENT MOI NHAT!
echo File cau hinh (agent_config.json) van duoc giu nguyen 100%%.
echo ===================================================================
echo.
timeout /t 3 >nul
