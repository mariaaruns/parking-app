@echo off
echo ============================================
echo   Starting ParkEase Parking App Servers
echo ============================================

:: 1. Ensure LocalDB is running
echo Checking SQL Server LocalDB...
sqllocaldb start MSSQLLocalDB >nul 2>&1

:: 2. Ensure Node.js is in PATH
where node >nul 2>&1
if %errorlevel% neq 0 (
    set "PATH=%LOCALAPPDATA%\Programs\nodejs-portable;%PATH%"
)

:: 3. Start Backend in a new window
echo Starting Backend API on http://localhost:5222...
start "ParkEase - Backend API" cmd /k "cd /d ""%~dp0backend"" && dotnet run --project src/ParkingApp.API/ParkingApp.API.csproj --launch-profile http"

:: 4. Start Frontend in a new window
echo Starting Frontend on http://localhost:4200...
start "ParkEase - Frontend" cmd /k "cd /d ""%~dp0frontend"" && npm start"

echo.
echo Both servers launched:
echo  - Frontend:   http://localhost:4200
echo  - Swagger UI: http://localhost:5222/swagger
echo.
