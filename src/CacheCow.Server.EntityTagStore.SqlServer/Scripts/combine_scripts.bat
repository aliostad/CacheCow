@echo off
cd .
echo. 2> script.sql
for %%x in (*.tbl) do call :merge %%x
for %%x in (*.sp) do call :merge %%x
goto :eof

:merge
copy  script.sql + %1 script.sql
exit /b