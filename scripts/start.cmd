@echo off
cd /d "%~dp0\.."
where dotnet >nul 2>nul || (echo Install the .NET 9 SDK first. & pause & exit /b 1)
where npm >nul 2>nul || (echo Install Node.js 22 or later first. & pause & exit /b 1)
dotnet restore SprintPilot.sln
if errorlevel 1 (pause & exit /b 1)
pushd frontend
call npm ci
if errorlevel 1 (popd & pause & exit /b 1)
popd
start "SprintPilot API - port 5092" cmd /k "dotnet run --project src/SprintPilot.Api --launch-profile SprintPilot.Api"
start "SprintPilot Frontend - port 5176" cmd /k "cd frontend && npm run dev"
echo Open http://localhost:5176 after both servers are ready.
echo Close both terminal windows to stop the application.
pause
