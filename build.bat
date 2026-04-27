@echo off
setlocal enabledelayedexpansion

:: ============================================================
:: Avryd Screen Reader — Build Script
:: Compiles all projects and produces AvrydSetup.exe
:: ============================================================

title Avryd Build

echo.
echo  ==========================================
echo        AVRYD SCREEN READER - BUILD
echo  ==========================================
echo.

:: -- Configuration ------------------------------------------
set CONFIG=Release
set DOTNET=dotnet
set NSIS=makensis
set DIST_DIR=%~dp0dist
set APP_OUT=%DIST_DIR%\app
set SLN=%~dp0Avryd.sln

:: -- Check prerequisites ------------------------------------
echo [1/6] Checking prerequisites...
where %DOTNET% >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo  ERROR: .NET SDK not found. Install from https://dot.net
    pause & exit /b 1
)
for /f "tokens=*" %%v in ('dotnet --version') do set DOTNET_VER=%%v
echo  .NET SDK: %DOTNET_VER%

:: -- Clean --------------------------------------------------
echo.
echo [2/6] Cleaning previous build...
if exist "%DIST_DIR%" rmdir /s /q "%DIST_DIR%"
mkdir "%DIST_DIR%"
mkdir "%APP_OUT%"
echo  Done.

:: -- Restore NuGet packages ---------------------------------
echo.
echo [3/6] Restoring packages...
%DOTNET% restore "%SLN%"
if %ERRORLEVEL% neq 0 ( echo  ERROR: Restore failed! & pause & exit /b 1 )
echo  Done.

:: -- Build (64-bit) -----------------------------------------
echo.
echo [4/6] Building Avryd Release x64...
%DOTNET% publish "%~dp0src\Avryd.App\Avryd.App.csproj" ^
    -c %CONFIG% -r win-x64 --self-contained false -o "%APP_OUT%"
if %ERRORLEVEL% neq 0 ( echo  ERROR: Build failed! & pause & exit /b 1 )
echo  Done.

:: -- Build (32-bit) -----------------------------------------
echo.
echo [5/6] Building Avryd Release x86 (32-bit)...
%DOTNET% publish "%~dp0src\Avryd.App\Avryd.App.csproj" ^
    -c %CONFIG% -r win-x86 --self-contained false -o "%DIST_DIR%\app_x86"
if %ERRORLEVEL% neq 0 (
    echo  WARNING: 32-bit build failed (continuing).
)

:: -- Copy resources -----------------------------------------
if exist "%~dp0resources" xcopy /s /q /i /y "%~dp0resources" "%APP_OUT%\resources" >nul
if exist "%~dp0misc\images"  xcopy /s /q /i /y "%~dp0misc\images"  "%APP_OUT%\Assets"   >nul

:: -- NSIS Installer -----------------------------------------
echo.
echo [6/6] Creating installer...
where %NSIS% >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo  NOTE: NSIS not found. Install from https://nsis.sourceforge.io
    echo  Then run:  makensis installer\setup.nsi
) else (
    %NSIS% "%~dp0installer\setup.nsi"
    if %ERRORLEVEL% neq 0 ( echo  ERROR: NSIS failed! & pause & exit /b 1 )
    echo  Installer: %DIST_DIR%\AvrydSetup.exe
)

echo.
echo  ==========================================
echo    BUILD COMPLETE
echo    App output:  %APP_OUT%
echo  ==========================================
echo.
pause
endlocal
