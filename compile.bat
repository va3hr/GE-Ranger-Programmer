@echo off
REM ==============================================
REM GE Ranger Programmer - WinForms Build Script
REM Run this from the ROOT project folder
REM ==============================================

:: Step 1: Create new WinForms project
dotnet new winforms -n GE-Ranger-Programmer -o ./src --force

:: Step 2: Copy your source files
copy .\src\*.cs .\src\GE-Ranger-Programmer\  >nul

:: Step 3: Build Release version
dotnet publish .\src\GE-Ranger-Programmer -c Release -r win-x64 --self-contained true -o ./publish

:: Step 4: Copy drivers
xcopy /Y .\Drivers\* .\publish\  >nul

echo.
echo ✅ Build complete! Check /publish/ folder for GE-Ranger-Programmer.exe
echo.
pause
