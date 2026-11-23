# AgriConnect – Farmer component (CSE5203 Service Oriented Computing)

.NET Framework 4.8 · WCF SOAP (`basicHttpBinding`) · Windows Forms · SQL Server (`AgriConnect_Farmer_SOC`)

## 1. Architecture and folder structure

```
Farmer/                               <- commit this whole folder into the team repo
├── Farmer.sln
├── README.md
├── Database/
│   └── 01_AgriConnect_Farmer_SOC.sql       CREATE DATABASE + tables + seed data
├── Farmer.Contracts/                       (class library – the SOAP contract)
│   ├── IFarmerService.cs                   [ServiceContract] + [OperationContract] + [FaultContract]
│   ├── AccountContracts.cs                 FarmerDto, FarmerRegistration, LoginCredentials, LoginResult
│   ├── SupplyContracts.cs                  SupplyRequestInput/Dto, OfficerDecisionInput
│   ├── QuotationOrderContracts.cs          QuotationDto, OrderConfirmationInput, ConfirmedOrderDto
│   ├── CropContracts.cs                    CropListingInput/Dto
│   ├── DashboardAndFaultContracts.cs       DashboardSummary, ServiceFault
│   └── FarmerConstants.cs                  XML namespaces + status constants
├── Farmer.Service/                         (console-hosted WCF service)
│   ├── FarmerService.cs                    business logic, validation, workflow rules, fault handling
│   ├── Program.cs                          ServiceHost (self-hosting, no IIS needed)
│   ├── App.config                          connection string, binding, metadata, other roles' endpoints
│   ├── Data/                               FarmerRepository (ADO.NET, parameterized), IFarmerRepository
│   ├── Integration/                        Supplier / Officer / Admin gateways (mock + real SOAP)
│   ├── Security/PasswordHasher.cs          salted SHA-256
│   └── Validation/InputValidator.cs
└── Farmer.Client/                          (Windows Forms – talks ONLY to the SOAP service)
    ├── Forms/                              LoginForm, RegisterForm, MainForm
    ├── Controls/                           Dashboard, SupplyRequests, Quotations (+orders), CropListings
    ├── ServiceCaller.cs                    one place that calls the generated proxy + handles faults
    └── UiHelper.cs / Session.cs / Program.cs
```

Layers: **WinForms client → (SOAP) → FarmerService → FarmerRepository → SQL Server**.
The client never touches SQL. The service reaches the other roles only through the gateway interfaces.

Workflow rules enforced in `FarmerService`: a request goes `Pending → Approved/Rejected → Confirmed` (or `Cancelled`);
quotations can only be read and an order only confirmed for **Approved** requests; the order price is re-read from the
supplier side (never trusted from the client); stock must cover the quantity; status change and order insert are one SQL transaction.

## 2. Database setup (SSMS)

1. Open SSMS, connect to your instance.
2. Open `Database/01_AgriConnect_Farmer_SOC.sql` → **Execute** (it drops and recreates the database).
3. The last query should show: Farmers 4, SupplyRequests 11, ConfirmedOrders 3, CropListings 9.
4. In `Farmer.Service/App.config` change `Data Source=.` if needed (`.\SQLEXPRESS`, `(localdb)\MSSQLLocalDB`, `localhost`).

Demo logins (password for all: `Farmer@123`): `nimal.perera@agriconnect.lk`, `kamala.silva@agriconnect.lk`,
`sunil.fernando@agriconnect.lk`, `dilani.jayasinghe@agriconnect.lk`.

## 3. Run the WCF service and check WSDL

1. Open `Farmer.sln` in Visual Studio (2019/2022, .NET Framework 4.8 targeting pack, “.NET desktop development” workload).
2. Right-click **Farmer.Service → Set as Startup Project → Debug → Start Without Debugging (Ctrl+F5)**.
3. A console window prints the endpoint and WSDL address. Leave it running.
4. Browse to `http://localhost:8733/Design_Time_Addresses/AgriConnect/Farmer/?wsdl` – you should see XML.
   (Port 8733/`Design_Time_Addresses` is pre-reserved by Windows, so no administrator rights are needed.
   If you change the address and get “Access denied”, run Visual Studio as administrator once.)

## 4. Add the Service Reference to the client (once)

1. With the service still running, right-click **Farmer.Client → Add → Service Reference…** (in VS 2022 it is under *Add → Connected Service* if the classic option is hidden; choose *WCF Web Service Reference* only if Service Reference is not offered).
2. Address: `http://localhost:8733/Design_Time_Addresses/AgriConnect/Farmer/` → **Go**.
3. Namespace: **`FarmerServiceReference`** (the code uses `Farmer.Client.FarmerServiceReference`). Click **OK**.
4. Optional: *Advanced…* → collection type `System.Collections.Generic.List` (code works with arrays too).
5. Build the solution. Set **Farmer.Client** as the startup project (or *Multiple startup projects*: Service → Start, Client → Start).

## 5. End-to-end test script (what to show in the viva)

| # | Feature | Steps | Expected |
|---|---|---|---|
| 1 | Register | Login screen → *Create account*, fill the form | “Account created”, email pre-filled on login |
| 2 | Validation | Register with weak password / duplicate email | Fault message from the service listing the errors |
| 3 | Login | `nimal.perera@agriconnect.lk` / `Farmer@123`; also try a wrong password | Main window opens; wrong password shows “Invalid email or password.” |
| 4 | Dashboard | Dashboard tab | Tiles: 3 requests, 1 pending, 1 approved, 1 confirmed order, 3 active listings; recent grids filled |
| 5 | Submit request | Supply Requests → fill item / qty / location / date → *Submit* | New row `Pending`; console logs `[MOCK Supplier]` and `[MOCK Officer]` notifications. Past date → validation error |
| 6 | Track status | Select the Pending row → *Check approval status* | Mock officer approves → status `Approved` (turn off with `MockOfficerAutoApprove=false`) |
| 7 | Cancel | Submit another request → *Cancel request* | Status `Cancelled`; a `Confirmed` request cannot be cancelled (workflow fault) |
| 8 | Compare quotations | Quotations & Orders → choose an approved request → *Load quotations* | 3 quotes; cheapest eligible is green; the third supplier is greyed (short on stock); orange note says placeholder data |
| 9 | Workflow guard | In WCF Test Client call `GetQuotationsForRequest(3)` (a Pending request) | Fault `WorkflowViolation` |
| 10 | Confirm order | Select the green quotation → *Confirm order…* → Yes | Order appears under “My confirmed orders”, request becomes `Confirmed`, dashboard counts update |
| 11 | Crop listings | Crop Listings → add listing, select it, edit, *Save changes*, *Close*, *Delete* | Each action reflected in the grid; closed listings cannot be edited |
| 12 | Marketplace | Select *Marketplace* and search `carrot` | Only Active listings of all farmers |
| 13 | Service down | Stop the service, click anything | “Cannot reach the Farmer service…” (no crash) |
| 14 | Officer callback | WCF Test Client → `ApplyOfficerDecision` (RequestId 3, Approved = true, IntegrationKey `agriconnect-demo-key`) | Request 3 becomes `Approved`; wrong key → `Unauthorized` |

WCF Test Client: `C:\Program Files\Microsoft Visual Studio\<version>\<edition>\Common7\IDE\WcfTestClient.exe`, then *File → Add Service* with the base address.

## 6. Connecting the teammates’ services

Everything about other roles is isolated in `Farmer.Service/Integration` and `App.config`:

1. Agree operations and addresses with the Supplier / Officer / Admin owners. `WcfGateways.cs` shows the operations this service expects (`ISupplierRemote`, `IOfficerRemote`, `IAdminRemote`). If their WSDL differs, add *their* Service Reference to Farmer.Service and adapt **only** the three `Wcf…Gateway` classes.
2. Put their addresses in `<client>` in `Farmer.Service/App.config`.
3. Set `UseMockIntegrations` to `false`.
4. Supplier quotations: if the Supplier endpoint fails, `GetQuotationsForRequest` falls back to clearly flagged placeholder quotations (never silently).
5. Officer → Farmer: the Officer service calls `ApplyOfficerDecision` (with the shared `IntegrationKey`) or the farmer clicks *Check approval status*, which pulls the decision.

## 7. Git: committing into the team repository

```
git clone <team-repo-url>
git checkout -b feature/farmer-component
# copy this Farmer/ folder into the repo root (keep .gitignore)
git add Farmer/Database          && git commit -m "Farmer: add SQL Server schema and seed data"
git add Farmer/Farmer.Contracts  && git commit -m "Farmer: add WCF data and service contracts"
git add Farmer/Farmer.Service    && git commit -m "Farmer: add repository, business logic, gateways and service host"
git add Farmer/Farmer.Client     && git commit -m "Farmer: add Windows Forms client"
git add Farmer/Farmer.sln Farmer/README.md Farmer/.gitignore && git commit -m "Farmer: add solution file and setup guide"
git push -u origin feature/farmer-component     # then open a pull request
```

Make several small, meaningful commits as you work (the marking rubric looks at commit history).
Do not commit `bin/`, `obj/`, `.vs/` or the generated `Service References/` folder if your team prefers to regenerate it – or commit it so teammates can build immediately; agree one rule as a team.
Never commit real passwords: `App.config` uses Windows authentication.

## 8. Design notes for the viva

- **Isolated database**: only `AgriConnect_Farmer_SOC`; `SupplierId`, `QuotationId`, `OfficerId` are plain INT references filled via SOAP.
- **Why SOAP/basicHttpBinding**: interoperable, WSDL-described contract that teammates can consume; `security mode="None"` is for the local demo – use `wsHttpBinding`/transport security (HTTPS) in production.
- **Fault contracts**: clients get a typed `ServiceFault` (ErrorCode, Message, ValidationErrors); raw exceptions and SQL errors never leave the service.
- **Security**: parameterized SQL only, salted password hashes, ownership checks on every farmer-scoped operation, shared integration key on the officer callback. Known limitation: no session token between client and service (farmer ID is sent per call) – add token or message security as future work.
- **Reusability**: gateway interfaces + repository interface (constructor injection), `ServiceCaller`/`UiHelper` on the client, UserControl per feature.
