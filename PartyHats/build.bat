@echo off
dotnet build -c Release
if errorlevel 1 (
    echo.
    echo Build failed!
    pause
    exit /b 1
)

set "SOURCE=bin\Release\netstandard2.1\PartyHats.dll"
set "DEST=..\BepInEx\plugins\PartyHats.dll"

if not exist "..\BepInEx\plugins" mkdir "..\BepInEx\plugins"
copy /Y "%SOURCE%" "%DEST%" >nul

echo Copied to: %DEST%
pause
