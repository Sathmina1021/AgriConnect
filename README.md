# AgriConnect - Service-Oriented Computing (SOC) Application
**Module:** CSE5203 - Service Oriented Computing (ICBT / Cardiff Metropolitan University)  
**Technology Stack (Aligned with Lecture Notes):**
- **Frontend UI:** ASP.NET Web Forms (`.aspx`, `.aspx.cs`, `Site.Master`) with ASP.NET Web Controls (`TextBox`, `DropDownList`, `Button`, `GridView`, `Label`) & Validation Controls (`RequiredFieldValidator`, `CompareValidator`, `RangeValidator`, `RegularExpressionValidator`, `ValidationSummary`).
- **Services Layer (SOC):** 4 Independent ASP.NET Web Services (`.asmx` / `.asmx.cs` using `[WebService]` and `[WebMethod]`) for each stakeholder role.
- **Architecture:** 3-Layer Architecture (Presentation Layer -> Business Logic Layer `BLL` -> Data Access Layer `DAL` using `System.Data.SqlClient`, `SqlConnection`, `SqlCommand`, `SqlDataAdapter`, `DataSet`).
- **Databases (Database-per-Service):** 4 Independent SQL Server Databases (`AgriConnect_FarmerDB`, `AgriConnect_SupplierDB`, `AgriConnect_OfficerDB`, `AgriConnect_AdminDB`).

## Folder Structure
- `DatabaseScripts/` - SQL Server `.sql` scripts to create all 4 independent databases and sample records.
- `Services/` - 4 Independent ASMX Web Service Projects (`.asmx` + `BLL` + `DAL` + `Models`):
  - `FarmerWebService/` (Port 5001)
  - `SupplierWebService/` (Port 5002)
  - `OfficerWebService/` (Port 5003)
  - `AdminWebService/` (Port 5004)
- `AgriConnectWebUI/` (Port 5000) - ASP.NET Web Forms Client Application (`Default.aspx`, `Farmer.aspx`, `Supplier.aspx`, `Officer.aspx`, `Admin.aspx`, `Site.Master`).

## How to Run in Visual Studio (Windows)
1. Open **SQL Server Management Studio (SSMS)** (or Visual Studio SQL Server Object Explorer) and run the 4 scripts inside `DatabaseScripts/` (`01_FarmerDB.sql` to `04_AdminDB.sql`).
2. Update the `ConnectionStrings` in `Web.config` if your SQL Server instance name is different from `(localdb)\MSSQLLocalDB`.
3. Open `AgriConnect.sln` in **Visual Studio 2019 / 2022**.
4. Right-click the Solution -> **Properties** -> **Multiple startup projects** -> Set all 5 projects (`FarmerWebService`, `SupplierWebService`, `OfficerWebService`, `AdminWebService`, `AgriConnectWebUI`) to **Start**.
5. Press **F5** (or **Ctrl + F5**) to run all 4 ASMX Web Services and the ASP.NET Web Forms Client Portal!
