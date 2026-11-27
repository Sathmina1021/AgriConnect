IF DB_ID('AgriConnect_AdminDB') IS NULL CREATE DATABASE AgriConnect_AdminDB;
GO
USE AgriConnect_AdminDB;
GO

IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE Users (
        UserId INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(50) NOT NULL UNIQUE,
        Password NVARCHAR(100) NOT NULL,
        FullName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(120) NOT NULL,
        Role NVARCHAR(30) NOT NULL,
        District NVARCHAR(80) NOT NULL,
        Active BIT NOT NULL DEFAULT 1
    );

    INSERT INTO Users (Username, Password, FullName, Email, Role, District, Active) VALUES
        (N'admin', N'admin123', N'D.M. Sathmina Mihikalpa', N'admin@agriconnect.lk', N'Admin', N'Colombo', 1),
        (N'farmer1', N'farmer123', N'Saman Perera', N'saman.perera@agriconnect.lk', N'Farmer', N'Homagama', 1),
        (N'supplier1', N'supplier123', N'Lanka Agro Supplies', N'contact@lankaagro.lk', N'Supplier', N'Kurunegala', 1),
        (N'officer1', N'officer123', N'Dr. Chaminda Silva', N'c.silva@agridept.gov.lk', N'Officer', N'Gampaha', 1);
END
GO

IF OBJECT_ID('dbo.AuditLogs', 'U') IS NULL
BEGIN
    CREATE TABLE AuditLogs (
        LogId INT IDENTITY(1,1) PRIMARY KEY,
        LogTime DATETIME NOT NULL DEFAULT GETDATE(),
        ServiceName NVARCHAR(60) NOT NULL,
        Action NVARCHAR(60) NOT NULL,
        Details NVARCHAR(300) NOT NULL
    );

    INSERT INTO AuditLogs (ServiceName, Action, Details) VALUES
        (N'SystemService', N'Startup', N'AgriConnect platform initialized with seed data.');
END
GO
