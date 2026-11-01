@echo off
setlocal
cd /d "%~dp0"
dotnet build "REPO.Cheat.csproj" --configuration Release
exit /b %ERRORLEVEL%
