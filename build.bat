REM build.bat
@echo off
echo Build Smart Bulb OPC UA Project
echo ===============================
dotnet restore
dotnet build
if %ERRORLEVEL% == 0 (
    echo.
    echo ✅ Build completata con successo!
    echo.
    echo Per avviare:
    echo 1. Esegui start-server.bat
    echo 2. In un altro terminale, esegui start-client.bat
    echo.
) else (
    echo.
    echo ❌ Build fallita! Controlla gli errori sopra.
)
pause