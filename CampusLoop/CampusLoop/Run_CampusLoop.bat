@echo off
title CampusLoop - College Marketplace
echo ========================================================
echo Starting CampusLoop Web Application...
echo Opening browser at http://localhost:5004 ...
echo ========================================================
cd /d "%~dp0"
timeout /t 2 /nobreak >nul
start "" "http://localhost:5004"
dotnet run --no-build
pause
