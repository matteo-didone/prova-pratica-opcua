REM start-server.bat
@echo off
echo Avvio Smart Bulb OPC UA Server
echo ==============================
echo Server URL: opc.tcp://localhost:4841/SmartBulbServer
echo.
cd SmartBulbOpcUa.Server
dotnet run
pause
