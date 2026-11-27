#!/usr/bin/env bash
set -e

dotnet build AgriConnect.sln

dotnet run --no-build --project Services/FarmerService &
P1=$!
dotnet run --no-build --project Services/SupplierService &
P2=$!
dotnet run --no-build --project Services/OfficerService &
P3=$!
dotnet run --no-build --project Services/AdminService &
P4=$!

sleep 2
dotnet run --no-build --project ClientApp &
P5=$!

trap "kill $P1 $P2 $P3 $P4 $P5 2>/dev/null || true" EXIT INT TERM
echo "AgriConnect running at http://localhost:5000"
wait
