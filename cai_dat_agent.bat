@echo off
setlocal
cd /d "%~dp0"

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [THONG BAO] Dang yeu cau quyen Administrator de cai dat va cau hinh ho tro UAC...
    powershell -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

title Cai Dat & Cau Hinh LAN Control Agent (Admin UAC Ready)

:: Tu dong cau hinh quyen UAC & UIPI cho phep dieu khien chuot phim xuyen qua cua so Admin
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" /v PromptOnSecureDesktop /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" /v EnableUIPI /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" /v ConsentPromptBehaviorAdmin /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" /v EnableSecureUIAPaths /t REG_DWORD /d 0 /f >nul 2>&1

if not exist "bin\LanControl.AgentSetup.exe" (
    if exist "LanControl.AgentSetup.exe" (
        start "" "LanControl.AgentSetup.exe"
        exit /b 0
    )
    echo [THONG BAO] Dang bien dich chuong trinh cai dat lan dau...
    call build.bat
)

if exist "bin\LanControl.AgentSetup.exe" (
    start "" "bin\LanControl.AgentSetup.exe"
) else (
    echo [LOI] Khong tim thay LanControl.AgentSetup.exe! Vui long kiem tra lai.
    pause
)
