@ECHO OFF
@SETLOCAL enableextensions
@CD /D "%~dp0"
TASKKILL /F /PID %1
IF EXIST ".\Resources\PoS.bin" DEL /Q /F ".\tmp\Resources\PoS.bin"
ROBOCOPY ".\tmp\ " . /E /Z /R:30 /W:1 /MOVE
RMDIR /Q /S  "%CD%\tmp\"
START "" "%CD%\JL.exe"
EXIT
