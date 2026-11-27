@echo off
echo Building AgriConnect solution...
dotnet build AgriConnect.sln
if errorlevel 1 (
    echo Build failed. Check your .NET SDK version in Directory.Build.props.
    pause
    exit /b 1
)

echo Starting AgriConnect Microservices...
start "FarmerService (5001)" cmd /k dotnet run --no-build --project Services\FarmerService
start "SupplierService (5002)" cmd /k dotnet run --no-build --project Services\SupplierService
start "OfficerService (5003)" cmd /k dotnet run --no-build --project Services\OfficerService
start "AdminService (5004)" cmd /k dotnet run --no-build --project Services\AdminService

timeout /t 3 /nobreak >nul
start "ClientApp Gateway (5000)" cmd /k dotnet run --no-build --project ClientApp

timeout /t 2 /nobreak >nul
start http://localhost:5000
echo All services started. Open http://localhost:5000 in your browser.
