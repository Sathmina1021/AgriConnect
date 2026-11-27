# AgriConnect Viva Preparation Guide

## Key Architectural Points to Explain

1. **Why Microservices / SOC instead of a Monolith?**
   - Each of the 4 stakeholders (Farmer, Supplier, Agriculture Officer, Admin) is built as a separate deployable ASP.NET Core project under `Services/`.
   - Team members can develop, test, and scale their service independently without breaking other roles.
   - Fault isolation: if `OfficerService` is temporarily offline, `FarmerService` and `SupplierService` continue serving their own data.

2. **Database-per-Service Pattern**
   - `FarmerService` owns `Services/FarmerService/Data/farmer_db.json` (and `Database/01_FarmerDB.sql`).
   - `SupplierService` owns `Services/SupplierService/Data/supplier_db.json` (and `Database/02_SupplierDB.sql`).
   - `OfficerService` owns `Services/OfficerService/Data/officer_db.json` (and `Database/03_OfficerDB.sql`).
   - `AdminService` owns `Services/AdminService/Data/admin_db.json` (and `Database/04_AdminDB.sql`).
   - Services never read another service's database file directly; all cross-service reads and updates happen over REST HTTP APIs (`ServiceClientHelper`).

3. **Stateless Security Across Services**
   - `AdminService` authenticates users via `/api/auth/login` and issues an `HttpOnly` cookie signed with HMAC-SHA256 (`AgriConnect.Shared.Security.TokenHelper`).
   - Every service independently verifies the signature and checks role permissions without querying a central session table.
   - Internal service-to-service endpoints require the `X-AgriConnect-Internal` header.

4. **API Gateway (`ClientApp`)**
   - Serves the web UI on `http://localhost:5000` and proxies `/api/farmer/*` (5001), `/api/supplier/*` (5002), `/api/officer/*` (5003), and `/api/admin/*` + `/api/auth/*` (5004).
   - Aggregates live health checks from all 4 services at `/api/health`.
