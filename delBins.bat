@echo off
REM ===============================================
REM CleanBinObj.bat
REM Deletes all bin and obj folders recursively
REM from a specified directory.
REM Usage: CleanBinObj.bat "C:\Path\To\Solution"
REM ===============================================

REM Check if a directory was provided
IF "%~1"=="" (
    echo Usage: %~nx0 "C:\Path\To\Solution"
    exit /b 1
)

SET "TARGET_DIR=%~1"

echo Deleting all bin and obj folders under "%TARGET_DIR%"...

REM Loop through all directories bottom-up
for /f "delims=" %%d in ('dir "%TARGET_DIR%" /b /s /ad ^| sort /r') do (
    if /i "%%~nxd"=="bin" (
        echo Deleting %%d
        rd /s /q "%%d"
    )
    if /i "%%~nxd"=="obj" (
        echo Deleting %%d
        rd /s /q "%%d"
    )
)

echo All bin and obj folders deleted.
pause
