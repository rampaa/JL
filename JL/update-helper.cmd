@ECHO OFF
@setlocal enableextensions  
@cd /d "%~dp0"
Robocopy ".\tmp\ " . /E /Z /MOVE
del "\"%CD%\tmp\""
start "" "%CD%\JL.exe"