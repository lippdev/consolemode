@echo off
cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -File "%~dp0ConsoleMode.ps1"
exit /b %ERRORLEVEL%
