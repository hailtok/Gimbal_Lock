@echo off
cd /d "%~dp0"
if exist "bin\Release\net8.0-windows\GimbalLockVisualizer.exe" (
    start "" "bin\Release\net8.0-windows\GimbalLockVisualizer.exe"
) else if exist "bin\Debug\net8.0-windows\GimbalLockVisualizer.exe" (
    start "" "bin\Debug\net8.0-windows\GimbalLockVisualizer.exe"
) else (
    dotnet run
)
