@ECHO OFF
@SETLOCAL enableextensions
@CD /D "%~dp0"
TASKKILL /F /T /PID %1
START "" "%CD%\JL.exe"
EXIT
