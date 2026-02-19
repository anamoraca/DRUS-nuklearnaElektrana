@echo off
setlocal EnableExtensions

REM ROOT = folder iznad "tools"
for %%I in ("%~dp0..") do set "ROOT=%%~fI"

REM Ako si u Release modu, promeni u Release
set "CONFIG=Debug"

set "EXE=%ROOT%\src\DUS.SensorClient\bin\%CONFIG%\net48\DUS.SensorClient.exe"

echo ROOT = %ROOT%
echo EXE  = %EXE%
echo.

if not exist "%EXE%" goto MISSING

echo Pokrecem 10 klijenata...
echo.

start "C101" "%EXE%" --clientId C101 --sensorId 1
start "C102" "%EXE%" --clientId C102 --sensorId 2
start "C103" "%EXE%" --clientId C103 --sensorId 3
start "C104" "%EXE%" --clientId C104 --sensorId 4
start "C105" "%EXE%" --clientId C105 --sensorId 5
start "C106" "%EXE%" --clientId C106 --sensorId 6
start "C107" "%EXE%" --clientId C107 --sensorId 7
start "C108" "%EXE%" --clientId C108 --sensorId 8
start "C109" "%EXE%" --clientId C109 --sensorId 9
start "C110" "%EXE%" --clientId C110 --sensorId 10

echo Gotovo.
goto END

:MISSING
echo [GRESKA] Ne postoji exe:
echo "%EXE%"
echo Resenje: Build Solution u Visual Studio i proveri Debug/Release.
pause

:END
endlocal
