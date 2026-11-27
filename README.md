# AgriConnect - Service-Oriented Computing (SOC) Platform

**Module:** CSE5203 - Service Oriented Computing (ICBT Campus / Cardiff Metropolitan University)

AgriConnect connects small-scale farmers, agricultural suppliers, government agriculture officers, and system administrators through a decoupled .NET microservices architecture.

## Folder Structure

```text
AgriConnect/
├── AgriConnect.sln
├── Directory.Build.props
├── start-all.bat
├── start-all.sh
├── Shared/
│   └── AgriConnect.Shared/        # Shared DTOs, HMAC token helper, JSON store, HTTP client helper
├── Services/
│   ├── FarmerService/             # Port 5001 — Farmer profiles, supply requests, crop listings (farmer_db.json)
│   ├── SupplierService/           # Port 5002 — Inventory, quotations, deliveries (supplier_db.json)
│   ├── OfficerService/            # Port 5003 — Request verification, agricultural advisories (officer_db.json)
│   └── AdminService/              # Port 5004 — Auth, user management, analytics, audit logs (admin_db.json)
├── ClientApp/                     # Port 5000 — API Gateway & multi-role web portal
├── Database/                      # SQL Server database-per-service scripts (01–04)
└── Docs/                          # SRS, Viva guide, and individual report reference
```

Each service under `Services/` follows a three-layer internal layout:
- `Controllers/` — REST endpoints and role authorization
- `Services/` — Business logic and inter-service HTTP calls
- `Data/` — Repository and isolated JSON database file (`<role>_db.json`)

## Prerequisites

- **.NET 10 SDK** (or update `<TargetFramework>` once in `Directory.Build.props` to `net8.0` or `net9.0` to match your installed SDK).
- No external NuGet packages or SQL Server setup required to run the prototype.

## Running the Project

### Windows
Double-click `start-all.bat` or run from Command Prompt:
```bat
start-all.bat
```

### macOS / Linux / Git Bash
```bash
chmod +x start-all.sh
./start-all.sh
```

Then open **http://localhost:5000**.

## Demo Accounts

| Role | Username | Password |
|---|---|---|
| Admin | `admin` | `admin123` |
| Farmer | `farmer1` | `farmer123` |
| Supplier | `supplier1` | `supplier123` |
| Officer | `officer1` | `officer123` |

## End-to-End Demo Flow

1. Sign in as **farmer1** and submit a new supply request or publish a crop listing.
2. Sign in as **supplier1**, view the open supply request fetched from `FarmerService`, and submit a quotation.
3. Sign in as **officer1**, review the request and quotations, approve or reject it, and publish an advisory.
4. Return to **farmer1**, compare quotations on the approved request, and confirm an order (automatically creates a delivery in `SupplierService`).
5. Return to **supplier1** and advance the delivery status (`Preparing` -> `Dispatched` -> `InTransit` -> `Delivered`).
6. Sign in as **admin** to monitor live health across all 4 services, manage user accounts, and view audit logs.
