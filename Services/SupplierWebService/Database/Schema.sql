-- AgriConnect : Supplier service database schema + demo data
-- Safe to run many times (IF ... IS NULL / IF NOT EXISTS guards).

IF OBJECT_ID('dbo.Suppliers','U') IS NULL
CREATE TABLE dbo.Suppliers (
    Id      INT IDENTITY(1,1) PRIMARY KEY,
    Name    NVARCHAR(100) NOT NULL,
    Region  NVARCHAR(50)  NOT NULL,
    Phone   NVARCHAR(30)  NULL,
    Email   NVARCHAR(100) NULL
);
GO

IF OBJECT_ID('dbo.InventoryItems','U') IS NULL
CREATE TABLE dbo.InventoryItems (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    SupplierId INT NOT NULL REFERENCES dbo.Suppliers(Id),
    ItemName   NVARCHAR(100) NOT NULL,
    StockQty   INT NOT NULL CHECK (StockQty >= 0),
    UnitPrice  DECIMAL(10,2) NOT NULL CHECK (UnitPrice > 0),
    UpdatedAt  DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- Local copy of supply requests received from the Farmer service (database-per-service pattern)
IF OBJECT_ID('dbo.SupplyRequests','U') IS NULL
CREATE TABLE dbo.SupplyRequests (
    RequestId        INT NOT NULL PRIMARY KEY,
    FarmerName       NVARCHAR(100) NOT NULL,
    ItemName         NVARCHAR(100) NOT NULL,
    Quantity         INT NOT NULL CHECK (Quantity > 0),
    DeliveryLocation NVARCHAR(150) NOT NULL,
    Region           NVARCHAR(50)  NOT NULL,
    PreferredDate    DATETIME NOT NULL,
    Status           NVARCHAR(20) NOT NULL DEFAULT 'Open',
    ReceivedAt       DATETIME NOT NULL DEFAULT GETDATE()
);
GO

IF OBJECT_ID('dbo.Quotations','U') IS NULL
CREATE TABLE dbo.Quotations (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    SupplyRequestId INT NOT NULL REFERENCES dbo.SupplyRequests(RequestId),
    SupplierId      INT NOT NULL REFERENCES dbo.Suppliers(Id),
    UnitPrice       DECIMAL(10,2) NOT NULL,
    StockAvailable  INT NOT NULL,
    DeliveryDays    INT NOT NULL,
    TotalPrice      DECIMAL(12,2) NOT NULL,
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Submitted',
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT UQ_Quotation_Request_Supplier UNIQUE (SupplyRequestId, SupplierId)
);
GO

IF OBJECT_ID('dbo.Deliveries','U') IS NULL
CREATE TABLE dbo.Deliveries (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    QuotationId     INT NOT NULL REFERENCES dbo.Quotations(Id),
    SupplyRequestId INT NOT NULL,
    SupplierId      INT NOT NULL,
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    UpdatedAt       DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- ---------------- Demo data ----------------
IF NOT EXISTS (SELECT 1 FROM dbo.Suppliers)
BEGIN
    INSERT INTO dbo.Suppliers (Name, Region, Phone, Email) VALUES
        (N'Green Valley Agro Supplies',      N'Western', N'011 2345678', N'sales@greenvalley.lk'),
        (N'Hill Country Seeds & Fertilizer', N'Central', N'081 2234567', N'info@hillcountry.lk');

    INSERT INTO dbo.InventoryItems (SupplierId, ItemName, StockQty, UnitPrice) VALUES
        (1, N'Urea Fertilizer 50kg',     200,  9500.00),
        (1, N'Paddy Seeds 10kg',         150,  4200.00),
        (1, N'Organic Compost 25kg',      80,  2800.00),
        (1, N'Pesticide Chlorpyrifos 1L', 12,  3600.00),
        (1, N'Hand Sprayer 16L',          40,  7500.00),
        (2, N'Vegetable Seed Pack',      300,   650.00),
        (2, N'NPK 15-15-15 50kg',        120, 11800.00),
        (2, N'Drip Irrigation Kit',       25, 18500.00);

    INSERT INTO dbo.SupplyRequests (RequestId, FarmerName, ItemName, Quantity, DeliveryLocation, Region, PreferredDate, Status) VALUES
        (1001, N'Nimal Perera',      N'Urea Fertilizer 50kg', 20, N'Piliyandala', N'Western', DATEADD(DAY, 7,  GETDATE()), N'Open'),
        (1002, N'Kamala Silva',      N'Paddy Seeds 10kg',     15, N'Horana',      N'Western', DATEADD(DAY, 10, GETDATE()), N'Open'),
        (1003, N'Sunil Bandara',     N'NPK 15-15-15 50kg',    10, N'Kandy',       N'Central', DATEADD(DAY, 5,  GETDATE()), N'Open'),
        (1004, N'Ruwan Fernando',    N'Organic Compost 25kg', 30, N'Kalutara',    N'Western', DATEADD(DAY, 14, GETDATE()), N'Open'),
        (1005, N'Dilani Jayasuriya', N'Hand Sprayer 16L',      5, N'Horana',      N'Western', DATEADD(DAY, 3,  GETDATE()), N'Closed');

    -- 1002: approved by the officer, waiting for the farmer to confirm
    INSERT INTO dbo.Quotations (SupplyRequestId, SupplierId, UnitPrice, StockAvailable, DeliveryDays, TotalPrice, Status)
        VALUES (1002, 1, 4200.00, 150, 3, 63000.00, N'Approved');
    -- 1005: accepted by the farmer, delivery already dispatched
    INSERT INTO dbo.Quotations (SupplyRequestId, SupplierId, UnitPrice, StockAvailable, DeliveryDays, TotalPrice, Status)
        VALUES (1005, 1, 7500.00, 40, 2, 37500.00, N'Accepted');

    INSERT INTO dbo.Deliveries (QuotationId, SupplyRequestId, SupplierId, Status)
        SELECT Id, SupplyRequestId, SupplierId, N'Dispatched' FROM dbo.Quotations WHERE SupplyRequestId = 1005;
END
GO
