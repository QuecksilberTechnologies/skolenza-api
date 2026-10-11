@echo off
setlocal
title Skolenza Policy Flow Simulator
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0local-server.ps1"
if errorlevel 1 (
  echo.
  echo Simulator could not start. Review the message above.
  pause
)
