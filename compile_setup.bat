@echo off
setlocal

:: ============================================================
:: Avryd — Compile Setup.exe Only
:: Run this after build.bat has already compiled the app.
:: Requires NSIS (makensis) to be installed.
:: ============================================================

title Avryd Installer Build

echo.
echo  ==========================================
echo   AVRYD - COMPILE INSTALLER (AvrydSetup.exe)
echo  ==========================================
echo.

set NSIS=makensis
set NSI_SCRIPT=%~dp0installer\setup.nsi
set DIST_DIR=%~dp0dist

where %NSIS% >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo  ERROR: makensis (NSIS) not found in PATH.
    echo  Install NSIS from https://nsis.sourceforge.io
    echo  Then re-run this script.
    pause
    exit /b 1
)

if not exist "%~dp0dist\app\Avryd.exe" (
    echo  ERROR: Avryd.exe not found in dist\app\
    echo  Run build.bat first to compile the application.
    pause
    exit /b 1
)

echo  Compiling installer...
%NSIS% "%NSI_SCRIPT%"
if %ERRORLEVEL% neq 0 (
    echo  ERROR: NSIS compilation failed!
    pause
    exit /b 1
)

echo.
echo  ==========================================
echo   SUCCESS: AvrydSetup.exe created in dist\
echo  ==========================================
echo.

if exist "%DIST_DIR%\AvrydSetup.exe" (
    echo  File size:
    for %%f in ("%DIST_DIR%\AvrydSetup.exe") do echo    %%~zf bytes
)

pause
endlocal
