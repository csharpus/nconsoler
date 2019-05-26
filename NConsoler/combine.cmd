@ECHO OFF

ECHO Combining source files into single one...

REM -- Delete output file if exists
IF EXIST %1 DEL %1

REM -- Create temporary target file with header
SET COMBINED_FILE=%TEMP%\NConsoler.cs
ECHO // NConsoler combined file > %COMBINED_FILE%
ECHO. >> %COMBINED_FILE%

REM -- Run for all .cs files in current folder and its subfolders
FOR /R %%F IN (*.cs) DO (
    ECHO.%%F | FIND /I "AssemblyInfo">NUL && (
        REM -- Skip AssemblyInfo file
        ECHO Skipping %%F...
    ) || (
        REM -- Combine all other files into one
        ECHO Adding %%F..
        ECHO // %%F >> %COMBINED_FILE%
        TYPE %%F >> %COMBINED_FILE%
        ECHO. >> %COMBINED_FILE%
        ECHO. >> %COMBINED_FILE%
    )
)

REM -- Move temporary file to final destination
MOVE %COMBINED_FILE% %1