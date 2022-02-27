@ECHO OFF
@setlocal enableextensions  
@cd /d "%~dp0"
TIMEOUT 5
Robocopy ".\tmp\ " . /E /Z /MOVE
rmdir /Q /S  "%CD%\tmp\"
start "" "%CD%\JL.exe"
