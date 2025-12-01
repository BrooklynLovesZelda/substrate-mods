@echo off
dotnet build -c Release -v quiet
if errorlevel 1 (
    echo.
    echo Build failed!
    pause
    exit /b 1
)

set "SOURCE=bin\Release\netstandard2.1\CellHUDMod.dll"
set "DEST=..\BepInEx\plugins\CellHUDMod.dll"

if not exist "..\BepInEx\plugins\CellHUDMod\" mkdir "..\BepInEx\plugins\CellHUDMod\"
copy /Y "%SOURCE%" "%DEST%" >nul

echo Copied to: %DEST%
pause
