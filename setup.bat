@echo off
setlocal enabledelayedexpansion

:: 1. Request Administrator Privileges
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo Requesting Administrator privileges...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

:: Ensure working directory is the script location after elevation
cd /d "%~dp0"

echo Welcome to Werewolf for Telegram Local Setup!
echo ===============================================

:: 2. Prompt for Telegram API Token
set /p API_TOKEN="Please enter your Telegram Bot API Token: "
if "%API_TOKEN%"=="" (
    echo Error: API Token cannot be empty.
    pause
    exit /b
)

:: 3. Setup Docker MSSQL Container
echo.
echo Starting MSSQL Docker container...
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Werewolf@12345" -p 1433:1433 --name werewolf-sql -d mcr.microsoft.com/mssql/server:2022-latest
if %errorLevel% neq 0 (
    echo Docker container might already be running or failed to start.
    docker start werewolf-sql
)

:: Wait for SQL Server to boot
echo Waiting for SQL Server to initialize (30 seconds)...
timeout /t 30 /nobreak >nul

:: 4. Initialize Database
echo.
echo Creating Database...
:: Convert Windows paths to Linux paths for Docker SQL Server
powershell -Command "(Get-Content 'werewolf.sql') -replace 'C:\\Program Files\\Microsoft SQL Server\\MSSQL12.SQLEXPRESS\\MSSQL\\DATA\\', '/var/opt/mssql/data/' | Set-Content '%TEMP%\werewolf_docker.sql'"

:: Copy and execute the modified script inside the container
docker cp "%TEMP%\werewolf_docker.sql" werewolf-sql:/var/opt/mssql/data/werewolf_docker.sql
:: Wait a bit more to be sure the DB is ready for queries
timeout /t 10 /nobreak >nul
docker exec -i werewolf-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U SA -P "Werewolf@12345" -i /var/opt/mssql/data/werewolf_docker.sql

:: 5. Set Registry Keys
echo.
echo Setting Windows Registry Keys...
reg add "HKLM\SOFTWARE\Werewolf" /v ProductionAPI /t REG_SZ /d "%API_TOKEN%" /f
set DB_CONN="metadata=res://*/WerewolfModel.csdl|res://*/WerewolfModel.ssdl|res://*/WerewolfModel.msl;provider=System.Data.SqlClient;provider connection string=\"data source=localhost,1433;initial catalog=werewolf;user id=SA;password=Werewolf@12345;MultipleActiveResultSets=True;App=EntityFramework;TrustServerCertificate=True\""
reg add "HKLM\SOFTWARE\Werewolf" /v BotConnectionString /t REG_SZ /d %DB_CONN% /f

:: 6. Build the Solution
echo.
echo Downloading NuGet...
if not exist "nuget.exe" (
    powershell -Command "Invoke-WebRequest -Uri 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile 'nuget.exe'"
)

echo Locating MSBuild...
set MSBUILD_PATH=
for /f "usebackq tokens=1* delims=: " %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
  set "MSBUILD_PATH=%%i:%%j"
)

if "%MSBUILD_PATH%"=="" (
    echo MSBuild not found using vswhere. Trying default paths...
    if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_PATH=%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_PATH=%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_PATH=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
)

if "%MSBUILD_PATH%"=="" (
    echo Error: Could not find MSBuild. Please ensure Visual Studio or Build Tools is installed.
    pause
    exit /b
)

echo MSBuild found: "%MSBUILD_PATH%"
echo Restoring NuGet packages...
nuget.exe restore "Werewolf for Telegram\WerewolfForTelegram.sln" -PackagesDirectory "Werewolf for Telegram\packages"

echo Compiling project...
"%MSBUILD_PATH%" "Werewolf for Telegram\WerewolfForTelegram.sln" /p:Configuration=Release /t:Build /m /p:RestorePackagesConfig=true

:: 7. Setup Directory Structure
echo.
echo Setting up server directories...
set ROOT_DIR=%~dp0Server
mkdir "%ROOT_DIR%\Control" 2>nul
mkdir "%ROOT_DIR%\Node 1" 2>nul
mkdir "%ROOT_DIR%\Logs" 2>nul
mkdir "%ROOT_DIR%\Languages" 2>nul

echo Copying compiled files...
xcopy /s /y "Werewolf for Telegram\Werewolf Control\bin\Release\*" "%ROOT_DIR%\Control\"
xcopy /s /y "Werewolf for Telegram\Werewolf Node\bin\Release\*" "%ROOT_DIR%\Node 1\"
xcopy /s /y "Werewolf for Telegram\Languages\*" "%ROOT_DIR%\Languages\"

:: 8. Start the applications
echo.
echo Setup Complete! Starting Werewolf Bot...
start "" "%ROOT_DIR%\Control\Werewolf Control.exe"
start "" "%ROOT_DIR%\Node 1\Werewolf Node.exe"

echo Both Control and Node have been launched.
pause
