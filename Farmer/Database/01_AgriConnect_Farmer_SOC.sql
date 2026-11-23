/* =====================================================================
   AgriConnect  -  FARMER role database
   Database : AgriConnect_Farmer_SOC   (isolated: NO links to Supplier /
              Officer / Admin databases. External IDs are plain INT
              reference columns, populated via SOAP calls only.)
   Run in   : SQL Server Management Studio (SSMS) -> New Query -> Execute
   Demo login for every seeded farmer: password  Farmer@123
   ===================================================================== */
USE master;
GO
IF DB_ID('AgriConnect_Farmer_SOC') IS NOT NULL
BEGIN
    ALTER DATABASE AgriConnect_Farmer_SOC SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE AgriConnect_Farmer_SOC;
END
GO
CREATE DATABASE AgriConnect_Farmer_SOC;
GO
USE AgriConnect_Farmer_SOC;
GO

/* ---------------------------- TABLES ------------------------------- */
CREATE TABLE dbo.Farmers
(
    FarmerId      INT IDENTITY(1,1)  NOT NULL CONSTRAINT PK_Farmers PRIMARY KEY,
    FullName      NVARCHAR(100)      NOT NULL,
    Email         NVARCHAR(150)      NOT NULL CONSTRAINT UQ_Farmers_Email UNIQUE,
    PasswordHash  VARCHAR(64)        NOT NULL,   -- SHA-256 hex of (salt + password)
    PasswordSalt  VARCHAR(32)        NOT NULL,   -- random per-user salt (hex)
    Phone         NVARCHAR(20)       NOT NULL,
    District      NVARCHAR(60)       NOT NULL,
    [Address]     NVARCHAR(250)      NULL,
    RegisteredOn  DATETIME2(0)       NOT NULL CONSTRAINT DF_Farmers_RegisteredOn DEFAULT SYSUTCDATETIME(),
    IsActive      BIT                NOT NULL CONSTRAINT DF_Farmers_IsActive DEFAULT 1
);
GO

CREATE TABLE dbo.SupplyRequests
(
    RequestId         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SupplyRequests PRIMARY KEY,
    FarmerId          INT               NOT NULL CONSTRAINT FK_SupplyRequests_Farmers REFERENCES dbo.Farmers(FarmerId),
    ItemName          NVARCHAR(100)     NOT NULL,
    Quantity          INT               NOT NULL CONSTRAINT CK_SupplyRequests_Qty CHECK (Quantity > 0),
    DeliveryLocation  NVARCHAR(200)     NOT NULL,
    PreferredDate     DATE              NOT NULL,
    [Status]          VARCHAR(20)       NOT NULL CONSTRAINT DF_SupplyRequests_Status DEFAULT 'Pending'
                      CONSTRAINT CK_SupplyRequests_Status CHECK ([Status] IN ('Pending','Approved','Rejected','Confirmed','Cancelled')),
    OfficerId         INT               NULL,    -- external reference (Agriculture Officer service)
    OfficerRemarks    NVARCHAR(300)     NULL,
    SupplierNotified  BIT               NOT NULL CONSTRAINT DF_SupplyRequests_SN DEFAULT 0,
    OfficerNotified   BIT               NOT NULL CONSTRAINT DF_SupplyRequests_ON DEFAULT 0,
    CreatedOn         DATETIME2(0)      NOT NULL CONSTRAINT DF_SupplyRequests_Created DEFAULT SYSUTCDATETIME(),
    UpdatedOn         DATETIME2(0)      NOT NULL CONSTRAINT DF_SupplyRequests_Updated DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_SupplyRequests_Farmer_Status ON dbo.SupplyRequests(FarmerId, [Status]);
GO

CREATE TABLE dbo.ConfirmedOrders
(
    OrderId               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ConfirmedOrders PRIMARY KEY,
    RequestId             INT           NOT NULL CONSTRAINT UQ_ConfirmedOrders_Request UNIQUE   -- one order per request
                          CONSTRAINT FK_ConfirmedOrders_Requests REFERENCES dbo.SupplyRequests(RequestId),
    FarmerId              INT           NOT NULL CONSTRAINT FK_ConfirmedOrders_Farmers REFERENCES dbo.Farmers(FarmerId),
    SupplierId            INT           NOT NULL,   -- external reference (Supplier service)
    SupplierName          NVARCHAR(100) NOT NULL,   -- snapshot at confirmation time
    QuotationId           INT           NOT NULL,   -- external reference (Supplier service)
    Quantity              INT           NOT NULL CONSTRAINT CK_ConfirmedOrders_Qty CHECK (Quantity > 0),
    UnitPrice             DECIMAL(12,2) NOT NULL CONSTRAINT CK_ConfirmedOrders_UP CHECK (UnitPrice >= 0),
    TotalPrice            DECIMAL(14,2) NOT NULL,
    EstimatedDeliveryDate DATE          NOT NULL,
    OrderStatus           VARCHAR(20)   NOT NULL CONSTRAINT DF_ConfirmedOrders_Status DEFAULT 'Confirmed'
                          CONSTRAINT CK_ConfirmedOrders_Status CHECK (OrderStatus IN ('Confirmed','Dispatched','Delivered','Cancelled')),
    SupplierNotified      BIT           NOT NULL CONSTRAINT DF_ConfirmedOrders_SN DEFAULT 0,
    OfficerNotified       BIT           NOT NULL CONSTRAINT DF_ConfirmedOrders_ON DEFAULT 0,
    ConfirmedOn           DATETIME2(0)  NOT NULL CONSTRAINT DF_ConfirmedOrders_On DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_ConfirmedOrders_Farmer ON dbo.ConfirmedOrders(FarmerId);
GO

CREATE TABLE dbo.CropListings
(
    ListingId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CropListings PRIMARY KEY,
    FarmerId         INT           NOT NULL CONSTRAINT FK_CropListings_Farmers REFERENCES dbo.Farmers(FarmerId),
    CropName         NVARCHAR(100) NOT NULL,
    Quantity         DECIMAL(10,2) NOT NULL CONSTRAINT CK_CropListings_Qty CHECK (Quantity > 0),
    Unit             NVARCHAR(20)  NOT NULL CONSTRAINT DF_CropListings_Unit DEFAULT 'kg',
    UnitPrice        DECIMAL(12,2) NOT NULL CONSTRAINT CK_CropListings_Price CHECK (UnitPrice > 0),
    AvailabilityDate DATE          NOT NULL,
    Notes            NVARCHAR(500) NULL,
    ContactInfo      NVARCHAR(150) NOT NULL,
    [Status]         VARCHAR(20)   NOT NULL CONSTRAINT DF_CropListings_Status DEFAULT 'Active'
                     CONSTRAINT CK_CropListings_Status CHECK ([Status] IN ('Active','Closed')),
    CreatedOn        DATETIME2(0)  NOT NULL CONSTRAINT DF_CropListings_Created DEFAULT SYSUTCDATETIME(),
    UpdatedOn        DATETIME2(0)  NOT NULL CONSTRAINT DF_CropListings_Updated DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_CropListings_Farmer ON dbo.CropListings(FarmerId);
CREATE INDEX IX_CropListings_Status ON dbo.CropListings([Status]);
GO

/* ---------------------------- SEED DATA ---------------------------- */
-- Password hash = SHA2_256( salt + 'Farmer@123' ), upper-case hex (matches PasswordHasher.cs)
INSERT INTO dbo.Farmers (FullName, Email, PasswordHash, PasswordSalt, Phone, District, [Address])
VALUES
(N'Nimal Perera',      N'nimal.perera@agriconnect.lk',
    CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'A1B2C3D4E5F60718293A4B5C6D7E8F90' + 'Farmer@123'), 2),
    'A1B2C3D4E5F60718293A4B5C6D7E8F90', N'0771234567', N'Gampaha',      N'45 Temple Road, Minuwangoda'),
(N'Kamala Silva',      N'kamala.silva@agriconnect.lk',
    CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'B2C3D4E5F60718293A4B5C6D7E8F90A1' + 'Farmer@123'), 2),
    'B2C3D4E5F60718293A4B5C6D7E8F90A1', N'0712345678', N'Kandy',        N'12 Hill View, Peradeniya'),
(N'Sunil Fernando',    N'sunil.fernando@agriconnect.lk',
    CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'C3D4E5F60718293A4B5C6D7E8F90A1B2' + 'Farmer@123'), 2),
    'C3D4E5F60718293A4B5C6D7E8F90A1B2', N'0759876543', N'Anuradhapura', N'88 Tank Road, Kekirawa'),
(N'Dilani Jayasinghe', N'dilani.jayasinghe@agriconnect.lk',
    CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'D4E5F60718293A4B5C6D7E8F90A1B2C3' + 'Farmer@123'), 2),
    'D4E5F60718293A4B5C6D7E8F90A1B2C3', N'0701122334', N'Nuwara Eliya', N'7 Greenfield Estate, Kandapola');
GO

-- Supply requests in every workflow stage (IDs 1..11 on a fresh database)
INSERT INTO dbo.SupplyRequests (FarmerId, ItemName, Quantity, DeliveryLocation, PreferredDate, [Status], OfficerId, OfficerRemarks, SupplierNotified, OfficerNotified)
VALUES
(1, N'Urea Fertilizer (kg)',          200,  N'Minuwangoda, Gampaha',   CAST(DATEADD(DAY, 14, GETDATE()) AS DATE), 'Confirmed', 9001, N'Approved - within seasonal fertilizer allocation.', 1, 1),  -- 1
(1, N'Paddy Seeds BG 300 (kg)',      150,  N'Minuwangoda, Gampaha',   CAST(DATEADD(DAY, 10, GETDATE()) AS DATE), 'Approved',  9001, N'Approved - certified seed variety.',               1, 1),  -- 2
(1, N'Organic Compost (kg)',         500,  N'Minuwangoda, Gampaha',   CAST(DATEADD(DAY, 12, GETDATE()) AS DATE), 'Pending',   NULL, NULL,                                                1, 1),  -- 3
(2, N'NPK 15-15-15 Fertilizer (kg)', 300,  N'Peradeniya, Kandy',      CAST(DATEADD(DAY, 9,  GETDATE()) AS DATE), 'Approved',  9002, N'Approved - suitable for vegetable plots.',         1, 1),  -- 4
(2, N'Drip Irrigation Kit (set)',      4,  N'Peradeniya, Kandy',      CAST(DATEADD(DAY, 21, GETDATE()) AS DATE), 'Confirmed', 9002, N'Approved - water-saving equipment.',               1, 1),  -- 5
(2, N'Neem Oil Pesticide (litre)',    20,  N'Peradeniya, Kandy',      CAST(DATEADD(DAY, 7,  GETDATE()) AS DATE), 'Rejected',  9002, N'Rejected - quantity exceeds permitted limit for the plot size. Reduce to 10 litres.', 1, 1), -- 6
(3, N'Hybrid Chilli Seeds (packet)',  50,  N'Kekirawa, Anuradhapura', CAST(DATEADD(DAY, 8,  GETDATE()) AS DATE), 'Pending',   NULL, NULL,                                                1, 1),  -- 7
(3, N'Mulching Sheets (roll)',        10,  N'Kekirawa, Anuradhapura', CAST(DATEADD(DAY, 15, GETDATE()) AS DATE), 'Confirmed', 9003, N'Approved.',                                       1, 1),  -- 8
(4, N'Carrot Seeds (kg)',              5,  N'Kandapola, Nuwara Eliya',CAST(DATEADD(DAY, 6,  GETDATE()) AS DATE), 'Approved',  9002, N'Approved - highland suitable variety.',            1, 1),  -- 9
(4, N'Potash Fertilizer (kg)',       250,  N'Kandapola, Nuwara Eliya',CAST(DATEADD(DAY, 11, GETDATE()) AS DATE), 'Pending',   NULL, NULL,                                                1, 1),  -- 10
(2, N'Tomato Seedlings (plant)',    1000,  N'Peradeniya, Kandy',      CAST(DATEADD(DAY, 5,  GETDATE()) AS DATE), 'Cancelled', NULL, NULL,                                                1, 1);  -- 11
GO

-- Confirmed orders for requests 1, 5, 8 (SupplierId / QuotationId are EXTERNAL references)
INSERT INTO dbo.ConfirmedOrders (RequestId, FarmerId, SupplierId, SupplierName, QuotationId, Quantity, UnitPrice, TotalPrice, EstimatedDeliveryDate, OrderStatus, SupplierNotified, OfficerNotified)
VALUES
(1, 1, 101, N'GreenGrow Supplies',  11, 200,   245.00, 49000.00, CAST(DATEADD(DAY, 5,  GETDATE()) AS DATE), 'Dispatched', 1, 1),
(5, 2, 102, N'Lanka AgroMart',      52,   4, 18500.00, 74000.00, CAST(DATEADD(DAY, 7,  GETDATE()) AS DATE), 'Confirmed',  1, 1),
(8, 3, 103, N'Ceylon Agro Traders', 83,  10,  4200.00, 42000.00, CAST(DATEADD(DAY, -2, GETDATE()) AS DATE), 'Delivered',  1, 1);
GO

INSERT INTO dbo.CropListings (FarmerId, CropName, Quantity, Unit, UnitPrice, AvailabilityDate, Notes, ContactInfo, [Status])
VALUES
(1, N'Nadu Rice (Paddy)', 800,  'kg',    120.00, CAST(DATEADD(DAY, 3,  GETDATE()) AS DATE), N'Freshly harvested, moisture below 14%.',        N'0771234567', 'Active'),
(1, N'Green Chilli',      150,  'kg',    280.00, CAST(DATEADD(DAY, 1,  GETDATE()) AS DATE), N'Hand-picked, no pesticide residue.',              N'0771234567', 'Active'),
(1, N'King Coconut',     2000,  'piece',  95.00, CAST(DATEADD(DAY, 5,  GETDATE()) AS DATE), N'Bulk orders welcome, delivery within Gampaha.',     N'nimal.perera@agriconnect.lk', 'Active'),
(2, N'Carrot',            400,  'kg',    190.00, CAST(DATEADD(DAY, 2,  GETDATE()) AS DATE), N'Grade A upcountry carrots.',                       N'0712345678', 'Active'),
(2, N'Pumpkin',           300,  'kg',    110.00, CAST(DATEADD(DAY, -4, GETDATE()) AS DATE), N'Season ended - listing closed.',                   N'0712345678', 'Closed'),
(3, N'Big Onion',        1000,  'kg',    320.00, CAST(DATEADD(DAY, 4,  GETDATE()) AS DATE), N'Dry-cured, suitable for storage.',                 N'0759876543', 'Active'),
(3, N'Banana (Ambul)',    500,  'kg',    150.00, CAST(DATEADD(DAY, 6,  GETDATE()) AS DATE), N'Ripening stage can be arranged.',                  N'0759876543', 'Active'),
(4, N'Leeks',             250,  'kg',    240.00, CAST(DATEADD(DAY, 2,  GETDATE()) AS DATE), N'Washed and bundled.',                              N'0701122334', 'Active'),
(4, N'Cabbage',           600,  'kg',    130.00, CAST(DATEADD(DAY, 3,  GETDATE()) AS DATE), N'Firm heads, 1.5-2 kg each.',                       N'0701122334', 'Active');
GO

/* Quick sanity check */
SELECT 'Farmers' AS TableName, COUNT(*) AS [Rows] FROM dbo.Farmers
UNION ALL SELECT 'SupplyRequests', COUNT(*) FROM dbo.SupplyRequests
UNION ALL SELECT 'ConfirmedOrders', COUNT(*) FROM dbo.ConfirmedOrders
UNION ALL SELECT 'CropListings', COUNT(*) FROM dbo.CropListings;
GO
