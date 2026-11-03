@echo off
cd /d "%~dp0"
set "DOTNET_CLI_HOME=E:\CODEX\tools\dotnet-home"
set "NUGET_PACKAGES=E:\CODEX\tools\nuget"
set "SDK=dotnet"
if exist "E:\CODEX\tools\dotnet\dotnet.exe" set "SDK=E:\CODEX\tools\dotnet\dotnet.exe"
"%SDK%" publish PcismoMaster.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist --nologo
if errorlevel 1 exit /b 1
echo Pronto: %~dp0dist\PCMasterUtility.exe
pause
