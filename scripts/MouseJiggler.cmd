@echo off
rem Double-clickable launcher for MouseJiggler.ps1 (bypasses per-user execution policy;
rem a machine-wide GPO policy still wins). Keeps the window open on failure so the
rem error message is readable.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0MouseJiggler.ps1" %*
if errorlevel 1 pause
