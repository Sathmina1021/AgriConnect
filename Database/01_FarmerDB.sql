IF DB_ID('AgriConnect_FarmerDB') IS NULL CREATE DATABASE AgriConnect_FarmerDB;
GO
USE AgriConnect_FarmerDB;
GO

IF OBJECT_ID('dbo.Farmers', 'U') IS NULL
BEGIN
    CREATE TABLE Farmers (
        FarmerId INT IDENTITY(1,1) PRIMARY KEY,
        FullName NVARCHAR(100) NOT NULL,
        FarmName NVARCHAR(120) NOT NULL,
        District NVARCHAR(80) NOT NULL,
        Phone NVARCHAR(20) NOT NULL,
        Acres FLOAT NOT NULL DEFAULT 1.0
    );

    INSERT INTO Farmers (FullName, FarmName, District, Phone, Acres) VALUES
        (N'Saman Perera', N'Perera Family Farm', N'Homagama', N'0771234567', 2.5),
        (N'Kamal Silva', N'Silva Agro Estate', N'Godagama', N'0772345678', 1.8),
        (N'Nimal Bandara', N'Bandara Paddy Fields', N'Anuradhapura', N'0773456789', 4.0);
END
GO

IF OBJECT_ID('dbo.SupplyRequests', 'U') IS NULL
BEGIN
    CREATE TABLE SupplyRequests (
        RequestId INT IDENTITY(1,1) PRIMARY KEY,
        FarmerId INT NOT NULL FOREIGN KEY REFERENCES Farmers(FarmerId),
        FarmerName NVARCHAR(100) NOT NULL,
        ItemName NVARCHAR(100) NOT NULL,
        Category NVARCHAR(50) NOT NULL,
        Quantity INT NOT NULL,
        Unit NVARCHAR(30) NOT NULL,
        DeliveryLocation NVARCHAR(100) NOT NULL,
        PreferredDate DATE NOT NULL,
        Status NVARCHAR(40) NOT NULL DEFAULT 'Pending',
        OfficerNote NVARCHAR(250) NULL
    );

    INSERT INTO SupplyRequests (FarmerId, FarmerName, ItemName, Category, Quantity, Unit, DeliveryLocation, PreferredDate, Status, OfficerNote) VALUES
        (1, N'Saman Perera', N'Urea Fertilizer', N'Fertilizer', 10, N'Bags', N'Homagama', DATEADD(DAY, 3, GETDATE()), N'Approved', N'Approved for agricultural use.'),
        (2, N'Kamal Silva', N'Hybrid Paddy Seeds', N'Seeds', 5, N'Bags', N'Godagama', DATEADD(DAY, 5, GETDATE()), N'Pending', NULL);
END
GO

IF OBJECT_ID('dbo.CropListings', 'U') IS NULL
BEGIN
    CREATE TABLE CropListings (
        CropId INT IDENTITY(1,1) PRIMARY KEY,
        FarmerId INT NOT NULL FOREIGN KEY REFERENCES Farmers(FarmerId),
        FarmerName NVARCHAR(100) NOT NULL,
        ContactPhone NVARCHAR(20) NOT NULL,
        CropName NVARCHAR(100) NOT NULL,
        QuantityKg FLOAT NOT NULL,
        PricePerKg DECIMAL(10,2) NOT NULL,
        Location NVARCHAR(100) NOT NULL,
        Status NVARCHAR(30) NOT NULL DEFAULT 'Available'
    );

    INSERT INTO CropListings (FarmerId, FarmerName, ContactPhone, CropName, QuantityKg, PricePerKg, Location, Status) VALUES
        (1, N'Saman Perera', N'0771234567', N'Red Nadu Rice', 500, 210.00, N'Homagama', N'Available'),
        (3, N'Nimal Bandara', N'0773456789', N'Organic Tomatoes', 120, 340.00, N'Anuradhapura', N'Available');
END
GO
