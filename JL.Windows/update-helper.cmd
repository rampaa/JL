@ECHO OFF
@SETLOCAL enableextensions
@CD /D "%~dp0"
TASKKILL /F /T /PID %1
ROBOCOPY ".\tmp\ " . /E /Z /R:30 /W:1 /MOVE
RMDIR /Q /S  "%CD%\tmp\"
START "" "%CD%\JL.exe"
EXIT
