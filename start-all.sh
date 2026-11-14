#!/bin/bash
dotnet run --project Services/FarmerService &
dotnet run --project Services/SupplierService &
dotnet run --project Services/OfficerService &
dotnet run --project Services/AdminService &
sleep 4
dotnet run --project ClientPortal &
wait
