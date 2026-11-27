IF DB_ID('AgriConnect_SupplierDB') IS NULL CREATE DATABASE AgriConnect_SupplierDB;
GO
USE AgriConnect_SupplierDB;
GO

IF OBJECT_ID('dbo.Inventory', 'U') IS NULL
BEGIN
    CREATE TABLE Inventory (
        ItemId INT IDENTITY(1,1) PRIMARY KEY,
        SupplierName NVARCHAR(100) NOT NULL,
        ProductName NVARCHAR(100) NOT NULL,
        Category NVARCHAR(50) NOT NULL,
        AvailableQty INT NOT NULL,
        Unit NVARCHAR(30) NOT NULL,
        UnitPrice DECIMAL(10,2) NOT NULL,
        Warehouse NVARCHAR(100) NOT NULL
    );

    INSERT INTO Inventory (SupplierName, ProductName, Category, AvailableQty, Unit, UnitPrice, Warehouse) VALUES
        (N'Lanka Agro Supplies', N'Urea Fertilizer', N'Fertilizer', 400, N'Bags', 850.00, N'Kurunegala'),
        (N'GreenField Distributors', N'Hybrid Paddy Seeds', N'Seeds', 150, N'Bags', 1200.00, N'Anuradhapura');
END
GO

IF OBJECT_ID('dbo.Quotations', 'U') IS NULL
BEGIN
    CREATE TABLE Quotations (
        QuotationId INT IDENTITY(1,1) PRIMARY KEY,
        SupplyRequestId INT NOT NULL,
        ItemName NVARCHAR(100) NOT NULL,
        SupplierName NVARCHAR(100) NOT NULL,
        ContactPhone NVARCHAR(20) NOT NULL,
        UnitPrice DECIMAL(10,2) NOT NULL,
        OfferedQty INT NOT NULL,
        DeliveryDays INT NOT NULL,
        Remarks NVARCHAR(250) NULL,
        TotalAmount DECIMAL(12,2) NOT NULL,
        Status NVARCHAR(30) NOT NULL DEFAULT 'Pending'
    );

    INSERT INTO Quotations (SupplyRequestId, ItemName, SupplierName, ContactPhone, UnitPrice, OfferedQty, DeliveryDays, Remarks, TotalAmount, Status) VALUES
        (1, N'Urea Fertilizer', N'Lanka Agro Supplies', N'0372229010', 850.00, 10, 2, N'Available in stock', 8500.00, N'Pending'),
        (1, N'Urea Fertilizer', N'GreenField Distributors', N'0372229044', 820.00, 10, 1, N'Fast delivery available', 8200.00, N'Pending');
END
GO

IF OBJECT_ID('dbo.Deliveries', 'U') IS NULL
BEGIN
    CREATE TABLE Deliveries (
        DeliveryId INT IDENTITY(1,1) PRIMARY KEY,
        SupplyRequestId INT NOT NULL,
        FarmerName NVARCHAR(100) NOT NULL,
        SupplierName NVARCHAR(100) NOT NULL,
        ItemName NVARCHAR(100) NOT NULL,
        Quantity NVARCHAR(50) NOT NULL,
        DeliveryLocation NVARCHAR(100) NOT NULL,
        Status NVARCHAR(40) NOT NULL DEFAULT 'Preparing',
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO
