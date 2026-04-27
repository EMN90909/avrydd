@echo off
setlocal enabledelayedexpansion

:: AVRYD65 Automated Build Script
:: This script compiles the C++ native components and publishes the .NET service.

set GCC_PATH=C:\ProgramData\mingw64\mingw64\bin\gcc.exe
set GXX_PATH=C:\ProgramData\mingw64\mingw64\bin\g++.exe
set DOTNET_PATH=dotnet
set ISCC_PATH="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

:: 1. Clean previous build
if exist dist rmdir /s /q dist
mkdir dist\publish

echo [1/4] Compiling Native Hooks (C++)...
%GCC_PATH% -shared -o src\Avryd65.Native\Avryd65.Native.dll src\Avryd65.Native\native_hooks.c -luser32 -lgdi32
if %errorlevel% neq 0 (
    echo ERROR: C++ Compilation failed.
    pause
    exit /b 1
)

echo [2/4] Publishing Core Services...
:: Publish the Launcher and Service to the same folder
%DOTNET_PATH% publish src\Avryd65.Launcher\Avryd65.Launcher.csproj -c Release -r win-x64 --self-contained true -o dist\publish -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
%DOTNET_PATH% publish src\Avryd65.Service\Avryd65.Service.csproj -c Release -r win-x64 --self-contained true -o dist\publish -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

echo [3/4] Finalizing Artifacts...
copy src\Avryd65.Native\Avryd65.Native.dll dist\publish\ >nul

echo [4/4] Building Accessible Installer...
if exist %ISCC_PATH% (
    %ISCC_PATH% setup.iss
) else (
    where iscc >nul 2>nul
    if !errorlevel! equ 0 (
        iscc setup.iss
    ) else (
        echo WARNING: Inno Setup not found. Cannot build .exe installer.
        echo Please install Inno Setup 6 or add ISCC.exe to your PATH.
    )
)

echo.
echo BUILD COMPLETE.
if exist dist\Avryd_setup.exe (
    echo SUCCESS: Installer created at dist\Avryd_setup.exe
)
pause
