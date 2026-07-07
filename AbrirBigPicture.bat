@echo off
:: === CONFIGURAÇÕES ===
set MMT_PATH="C:\MultiMonitorTool\MultiMonitorTool.exe"
set MONITOR_TV=3
set MONITOR_MAIN=1

:: 1. Liga o monitor 3 e define como principal
%MMT_PATH% /TurnOn %MONITOR_TV%
%MMT_PATH% /SetPrimary %MONITOR_TV%
timeout /t 2 /nobreak >nul

:: 2. Abre a Steam no modo Big Picture
start steam://open/bigpicture

:: 3. Executa o PowerShell e espera ele terminar por completo
start /wait "" powershell -ExecutionPolicy Bypass -WindowStyle Hidden -File "%~dp0telas_pretas.ps1"

:: 4. RESTAURAÇÃO DO SETUP
%MMT_PATH% /SetPrimary %MONITOR_MAIN%
timeout /t 1 /nobreak >nul
%MMT_PATH% /TurnOff %MONITOR_TV%

exit