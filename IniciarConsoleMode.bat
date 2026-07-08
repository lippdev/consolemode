@echo off
chcp 65001 >nul
cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -NoProfile -File "%~dp0ConsoleMode.ps1"
exit /b %ERRORLEVEL%
