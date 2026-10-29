@echo off
setlocal enabledelayedexpansion

echo ===================================================================
echo     BUILDING LAN CONTROL MULTI-VLAN CLIENT-SERVER SUITE (.NET 4.8)
echo ===================================================================
echo.

set CSC="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist %CSC% (
    set CSC="C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

if not exist %CSC% (
    echo [ERROR] C# Compiler csc.exe not found in Microsoft.NET Framework directories!
    pause
    exit /b 1
)

if not exist "bin" mkdir "bin"
if not exist "setup" mkdir "setup"

:: Clean up legacy NetSupport binaries if present
del /f /q "NetSupport*.exe" "NetSupport*.dll" >nul 2>&1
del /f /q "bin\NetSupport*.exe" "bin\NetSupport*.dll" >nul 2>&1
del /f /q "setup\NetSupport*.exe" "setup\NetSupport*.dll" >nul 2>&1

echo [1/7] Compiling LanControl.Core.dll...
%CSC% /nologo /target:library /out:bin\LanControl.Core.dll /r:System.Web.Extensions.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Management.dll /recurse:src\NetSupport.Core\*.cs
if %ERRORLEVEL% neq 0 (
    echo [FAIL] Failed to build LanControl.Core.dll
    exit /b %ERRORLEVEL%
)
echo      [OK] LanControl.Core.dll built successfully.

echo [2/7] Compiling LanControl.Gateway.exe...
%CSC% /nologo /win32icon:icon.ico /target:winexe /out:bin\LanControl.Gateway.exe /r:bin\LanControl.Core.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /recurse:src\NetSupport.Gateway\*.cs
if %ERRORLEVEL% neq 0 (
    echo [FAIL] Failed to build LanControl.Gateway.exe
    exit /b %ERRORLEVEL%
)
echo      [OK] LanControl.Gateway.exe built successfully.

echo [3/7] Compiling LanControl.Agent.exe...
%CSC% /nologo /win32icon:icon.ico /target:winexe /out:bin\LanControl.Agent.exe /r:bin\LanControl.Core.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.Management.dll /recurse:src\NetSupport.Agent\*.cs
if %ERRORLEVEL% neq 0 (
    echo [FAIL] Failed to build LanControl.Agent.exe
    exit /b %ERRORLEVEL%
)
echo      [OK] LanControl.Agent.exe built successfully.

echo [4/7] Compiling LanControl.Control.exe...
%CSC% /nologo /win32icon:icon.ico /target:winexe /out:bin\LanControl.Control.exe /r:bin\LanControl.Core.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /recurse:src\NetSupport.Control\*.cs
if %ERRORLEVEL% neq 0 (
    echo [FAIL] Failed to build LanControl.Control.exe
    exit /b %ERRORLEVEL%
)
echo      [OK] LanControl.Control.exe built successfully.

echo [5/7] Compiling LanControl.AgentSetup.exe...
%CSC% /nologo /win32icon:icon.ico /target:winexe /out:bin\LanControl.AgentSetup.exe /r:bin\LanControl.Core.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /recurse:src\NetSupport.Installer\*.cs
if %ERRORLEVEL% neq 0 (
    echo [FAIL] Failed to build LanControl.AgentSetup.exe
    exit /b %ERRORLEVEL%
)
echo      [OK] LanControl.AgentSetup.exe built successfully.

echo [6/7] Compiling LanControl.ServerSetup.exe...
%CSC% /nologo /win32icon:icon.ico /target:winexe /out:bin\LanControl.ServerSetup.exe /r:bin\LanControl.Core.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /recurse:src\NetSupport.ServerInstaller\*.cs
if %ERRORLEVEL% neq 0 (
    echo [FAIL] Failed to build LanControl.ServerSetup.exe
    exit /b %ERRORLEVEL%
)
echo      [OK] LanControl.ServerSetup.exe built successfully.

echo [7/7] Compiling setup\LanControlSetup.exe (1-Click Unified Installer)...
%CSC% /nologo /win32icon:icon.ico /target:winexe /out:setup\LanControlSetup.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /recurse:src\NetSupport.SetupWizard\*.cs
if %ERRORLEVEL% neq 0 (
    echo [FAIL] Failed to build setup\LanControlSetup.exe
    exit /b %ERRORLEVEL%
)
echo      [OK] setup\LanControlSetup.exe built successfully.

copy /y setup\LanControlSetup.exe setup\Setup.exe >nul
copy /y setup\LanControlSetup.exe bin\LanControlSetup.exe >nul
copy /y setup\LanControlSetup.exe LanControlSetup.exe >nul
if exist icon.ico copy /y icon.ico bin\icon.ico >nul
if exist icon.ico copy /y icon.ico setup\icon.ico >nul
if exist about.md copy /y about.md bin\about.md >nul
if exist about.md copy /y about.md setup\about.md >nul
if exist HUONG_DAN_UPDATE.md copy /y HUONG_DAN_UPDATE.md bin\HUONG_DAN_UPDATE.md >nul
if exist HUONG_DAN_UPDATE.md copy /y HUONG_DAN_UPDATE.md setup\HUONG_DAN_UPDATE.md >nul
copy /y bin\LanControl.Core.dll setup\ >nul
copy /y bin\LanControl.Agent.exe setup\ >nul
copy /y bin\LanControl.Gateway.exe setup\ >nul
copy /y bin\LanControl.Control.exe setup\ >nul
copy /y bin\LanControl.Core.dll . >nul
copy /y bin\LanControl.Gateway.exe . >nul
copy /y bin\LanControl.Control.exe . >nul

echo.
echo ===================================================================
echo [SUCCESS] ALL LAN CONTROL COMPONENTS COMPILED INTO bin\ AND setup\ !
echo ===================================================================
echo   [BIN FOLDER]
echo   - bin\icon.ico                   (Application Icon)
echo   - bin\about.md                   (About / Documentation)
echo   - bin\HUONG_DAN_UPDATE.md        (Update Guide)
echo   - bin\LanControl.Core.dll        (Shared Protocols and Utilities)
echo   - bin\LanControl.Gateway.exe     (Central Multi-VLAN Relay Server)
echo   - bin\LanControl.Agent.exe       (Client Service / Workstation Agent)
echo   - bin\LanControl.Control.exe     (Master Management Console)
echo   - bin\LanControl.AgentSetup.exe  (Agent Quick Installer)
echo   - bin\LanControl.ServerSetup.exe (Server Quick Installer)
echo.
echo   [SETUP FOLDER - STANDALONE DEPLOYMENT USB PACKAGE]
echo   - setup\LanControlSetup.exe      (1-Click Unified Setup)
echo   - setup\Setup.exe                (Convenience shortcut)
echo   - setup\cai_dat.bat              (1-Click Launcher)
echo   - setup\cap_nhat_nhanh.bat       (1-Click Fast Updater)
echo   - setup\HUONG_DAN_UPDATE.md      (Update Guide)
echo   - setup\icon.ico                 (LAN Control High-Res Icon)
echo   - setup\about.md                 (Product Documentation)
echo   - setup\*.exe, *.dll             (Full Offline Payload Files)
echo ===================================================================
echo.
