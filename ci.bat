@echo off
setlocal

cd /d "%~dp0"

echo [1/4] restore...
dotnet restore SysmacDataTraceViewer.sln
if errorlevel 1 goto :error

echo [2/4] build...
dotnet build SysmacDataTraceViewer.sln -c Release --no-restore -warnaserror
if errorlevel 1 goto :error

echo [3/4] format and analyzer checks...
dotnet format SysmacDataTraceViewer.sln whitespace --verify-no-changes --no-restore
if errorlevel 1 goto :error
dotnet format SysmacDataTraceViewer.sln style --verify-no-changes --severity warn --no-restore
if errorlevel 1 goto :error
dotnet format SysmacDataTraceViewer.sln analyzers --verify-no-changes --severity warn --no-restore
if errorlevel 1 goto :error

echo [4/4] publish smoke test...
call build.bat win-x64 artifacts\ci
if errorlevel 1 goto :error
if not exist artifacts\ci\SysmacDataTraceViewer.exe goto :error

echo CI checks passed.
exit /b 0

:error
echo CI checks failed.
exit /b 1
