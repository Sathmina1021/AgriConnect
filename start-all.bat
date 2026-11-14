@echo off
start "FarmerService" cmd /k "dotnet run --project Services/FarmerService"
start "SupplierService" cmd /k "dotnet run --project Services/SupplierService"
start "OfficerService" cmd /k "dotnet run --project Services/OfficerService"
start "AdminService" cmd /k "dotnet run --project Services/AdminService"
timeout /t 4 >nul
start "ClientPortal" cmd /k "dotnet run --project ClientPortal"
