@echo off
REM Build and run the Ice Cream Parlour application
cd /d %~dp0

echo ===============================
echo Ice Cream Parlour Project
echo ===============================
echo Starting build...
dotnet build
if %errorlevel% neq 0 (
    echo Build failed!
    pause
    exit /b %errorlevel%
)

echo Build successful!
echo Starting application on http://localhost:5000
echo Press Ctrl+C to stop.

dotnet run --urls http://localhost:5000
@echo off
cd /d F:\.net\Ice_Cream_Parlour_Eproject\Ice_Cream_Parlour_Eproject
dotnet run --urls http://localhost:5000