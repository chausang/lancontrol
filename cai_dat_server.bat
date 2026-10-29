@echo off
setlocal
cd /d "%~dp0"

title Cai Dat & Cau Hinh LAN Control Server Gateway

if not exist "bin\LanControl.ServerSetup.exe" (
    if exist "LanControl.ServerSetup.exe" (
        start "" "LanControl.ServerSetup.exe"
        exit /b 0
    )
    echo [THONG BAO] Dang bien dich chuong trinh cai dat Server lan dau...
    call build.bat
)

if exist "bin\LanControl.ServerSetup.exe" (
    start "" "bin\LanControl.ServerSetup.exe"
) else (
    echo [LOI] Khong tim thay LanControl.ServerSetup.exe! Vui long kiem tra lai.
    pause
)
