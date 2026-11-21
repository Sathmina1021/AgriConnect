-- AgriConnect - Agriculture Officer database (SQL Server / LocalDB)
-- Run this whole script once in SSMS or Visual Studio (SQL Server Object Explorer).
-- Running it again deletes and recreates everything.

IF DB_ID('AgriOfficerDb') IS NULL
    CREATE DATABASE AgriOfficerDb;
GO

USE AgriOfficerDb;
GO

DROP TABLE IF EXISTS ComplianceChecks;
DROP TABLE IF EXISTS Quotations;
DROP TABLE IF EXISTS VerificationCases;
DROP TABLE IF EXISTS Recommendations;
DROP TABLE IF EXISTS ComplianceAlerts;
DROP TABLE IF EXISTS ReferencePrices;
DROP TABLE IF EXISTS Officers;
GO

CREATE TABLE Officers (
    OfficerId     INT IDENTITY(1,1) PRIMARY KEY,
    Username      NVARCHAR(50)  NOT NULL UNIQUE,
    PasswordHash  NVARCHAR(64)  NOT NULL,          -- SHA-256 hash, never the plain password
    FullName      NVARCHAR(100) NOT NULL,
    District      NVARCHAR(50)  NOT NULL
);

CREATE TABLE ReferencePrices (
    ItemName  NVARCHAR(100) PRIMARY KEY,
    Price     DECIMAL(18,2) NOT NULL               -- normal market price per unit (LKR)
);

CREATE TABLE VerificationCases (
    CaseId            INT IDENTITY(1,1) PRIMARY KEY,
    SupplyRequestId   NVARCHAR(30)   NOT NULL UNIQUE,   -- id used in the Farmer service
    FarmerId          NVARCHAR(30)   NOT NULL,
    FarmerName        NVARCHAR(100)  NOT NULL,
    ItemName          NVARCHAR(100)  NOT NULL,
    Quantity          INT            NOT NULL,
    DeliveryLocation  NVARCHAR(150)  NULL,
    PreferredDate     DATE           NOT NULL,
    Status            NVARCHAR(20)   NOT NULL DEFAULT 'Pending',  -- Pending / Approved / Rejected / ChangesRequested
    DecisionNote      NVARCHAR(1000) NULL,
    DecidedBy         NVARCHAR(100)  NULL,
    CreatedAt         DATETIME       NOT NULL DEFAULT GETDATE(),
    DecidedAt         DATETIME       NULL,
    NotificationSent  BIT            NOT NULL DEFAULT 0
);

CREATE TABLE Quotations (
    QuotationId    INT IDENTITY(1,1) PRIMARY KEY,
    CaseId         INT            NOT NULL REFERENCES VerificationCases(CaseId) ON DELETE CASCADE,
    SupplierId     NVARCHAR(30)   NOT NULL,
    SupplierName   NVARCHAR(100)  NOT NULL,
    UnitPrice      DECIMAL(18,2)  NOT NULL,
    StockQuantity  INT            NOT NULL,
    DeliveryDays   INT            NOT NULL
);

CREATE TABLE ComplianceChecks (
    CheckId   INT IDENTITY(1,1) PRIMARY KEY,
    CaseId    INT            NOT NULL REFERENCES VerificationCases(CaseId) ON DELETE CASCADE,
    RuleName  NVARCHAR(60)   NOT NULL,
    Passed    BIT            NOT NULL,
    Detail    NVARCHAR(300)  NOT NULL
);

CREATE TABLE Recommendations (
    RecommendationId  INT IDENTITY(1,1) PRIMARY KEY,
    FarmerId          NVARCHAR(30)   NOT NULL,
    FarmerName        NVARCHAR(100)  NOT NULL,
    Category          NVARCHAR(60)   NOT NULL,
    Title             NVARCHAR(120)  NOT NULL,
    Advice            NVARCHAR(2000) NOT NULL,
    OfficerName       NVARCHAR(100)  NOT NULL,
    CreatedAt         DATETIME       NOT NULL DEFAULT GETDATE()
);

CREATE TABLE ComplianceAlerts (
    AlertId       INT IDENTITY(1,1) PRIMARY KEY,
    SupplierId    NVARCHAR(30)   NOT NULL,
    SupplierName  NVARCHAR(100)  NOT NULL,
    Description   NVARCHAR(500)  NOT NULL,
    Severity      NVARCHAR(10)   NOT NULL,           -- Low / Medium / High
    IsResolved    BIT            NOT NULL DEFAULT 0,
    RaisedBy      NVARCHAR(100)  NOT NULL,
    CreatedAt     DATETIME       NOT NULL DEFAULT GETDATE(),
    ResolvedAt    DATETIME       NULL
);
GO

-- ---------- Sample data ----------
-- Login: officer1 / Officer@123
INSERT INTO Officers (Username, PasswordHash, FullName, District)
VALUES ('officer1', '', 'Kamal Perera', 'Kandy');

INSERT INTO ReferencePrices (ItemName, Price) VALUES
    ('Urea', 270), ('TSP', 320), ('Paddy Seeds', 180), ('Neem Oil', 1500);

INSERT INTO ComplianceAlerts (SupplierId, SupplierName, Description, Severity, IsResolved, RaisedBy, CreatedAt, ResolvedAt) VALUES
    ('SUP-02', 'FarmLink',            'Quoted more than 50% above the reference price for Urea on three requests.', 'High',   0, 'Kamal Perera', DATEADD(DAY, -3,  GETDATE()), NULL),
    ('SUP-03', 'AgroMart',            'Two late deliveries reported by farmers this month.',                        'Medium', 0, 'Kamal Perera', DATEADD(DAY, -5,  GETDATE()), NULL),
    ('SUP-01', 'GreenGrow Suppliers', 'Stock certificate renewal was overdue.',                                    'Low',    1, 'Kamal Perera', DATEADD(DAY, -20, GETDATE()), DATEADD(DAY, -12, GETDATE()));

INSERT INTO Recommendations (FarmerId, FarmerName, Category, Title, Advice, OfficerName, CreatedAt) VALUES
    ('F-001', 'Nimal Silva',       'Fertilizer',   'Split the urea application', 'Apply urea in two or three splits during the season to reduce losses in heavy rain.', 'Kamal Perera', DATEADD(DAY, -4, GETDATE())),
    ('F-003', 'Ruwan Jayasinghe',  'Pest control', 'Neem oil dilution',          'Dilute neem oil to 0.5 percent and spray in the evening to protect pollinators.',    'Kamal Perera', DATEADD(DAY, -1, GETDATE()));
GO
