@echo off
REM ======================================================
REM InitGitInteractive.bat
REM Initialize Git repository for a .NET solution interactively
REM ======================================================

REM Prompt for solution folder (default: current folder)
set /p SOL_FOLDER="Enter full path to solution folder (default: current folder): "
if "%SOL_FOLDER%"=="" set SOL_FOLDER=%cd%
echo Using solution folder: %SOL_FOLDER%
cd /d "%SOL_FOLDER%" || (
    echo Failed to cd into folder. Exiting.
    exit /b 1
)

REM Prompt for GitHub username
set /p GH_USER="Enter your GitHub username: "

REM Prompt for GitHub repository name
set /p GH_REPO="Enter the GitHub repository name: "

echo.
echo Initializing Git repository...
git init

echo.
echo Creating .gitignore for .NET...
(
echo bin/
echo obj/
echo Generated/
echo *.user
echo *.suo
echo *.vs
echo *.Backup.tmp
echo *.Designer.cs.bak
echo **/appsettings.json
) > .gitignore

echo.
echo Adding all files to Git...
git add .

echo.
echo Committing initial version...
git commit -m "Initial commit of %GH_REPO% solution"

echo.
echo Adding GitHub remote...
git remote add origin https://github.com/%GH_USER%/%GH_REPO%.git

echo.
echo Renaming branch to main...
git branch -M main

echo.
echo Pushing to GitHub...
git push -u origin main

echo.
echo Git repository initialized and pushed successfully!
pause
