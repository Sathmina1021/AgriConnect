# SupplierWebService (Supplier role)

ASMX SOAP service (.NET Framework 4.8): inventory management, quotation processing, delivery status, supplier dashboard.
Layers: `SupplierWS.asmx` (service) -> `BLL/` -> `DAL/` (ADO.NET) -> database `AgriConnect_SupplierDB`.

* Port **5002** - WSDL: `http://localhost:5002/SupplierWS.asmx?WSDL`
* Database + tables + demo data are created automatically on first run (also available as `DatabaseScripts/02_SupplierDB.sql`).
  Check the server name in `Web.config` (default `(localdb)\MSSQLLocalDB`).
* All methods except `CheckHealth` need the SOAP header `AuthHeader.ApiKey` (value in `Web.config`).

## Methods other services call
| Caller | Method | Purpose |
|---|---|---|
| Farmer service | `ReceiveSupplyRequest(SupplyRequest)` | new supply request reaches regional suppliers |
| Officer service | `UpdateQuotationStatus(id, "Approved" / "Rejected")` | officer decision on a quotation |
| Farmer service | `UpdateQuotationStatus(id, "Accepted")` | farmer confirms; creates delivery, reduces stock |

Quotation flow: Submitted -> Approved -> Accepted (or Rejected). Delivery: Pending -> Dispatched -> InTransit -> Delivered.

## Other methods (Supplier UI)
GetSuppliers, GetInventory, AddInventoryItem, UpdateInventoryItem, DeleteInventoryItem, GetOpenRequests,
SubmitQuotation, GetMyQuotations, GetDeliveries, UpdateDeliveryStatus, GetDashboardSummary, CheckHealth.

`SupplierDesktopClient/` is an optional Windows Forms test client for this service (not part of AgriConnect.sln).
