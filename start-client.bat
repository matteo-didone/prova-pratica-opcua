REM start-client.bat  
@echo off
echo Avvio Smart Bulb OPC UA Client
echo ==============================
echo NOTA: Assicurati che il server sia già avviato!
echo.
timeout /t 3
cd SmartBulbOpcUa.Client
dotnet run
pause