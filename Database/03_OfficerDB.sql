IF DB_ID('AgriConnect_OfficerDB') IS NULL CREATE DATABASE AgriConnect_OfficerDB;
GO
USE AgriConnect_OfficerDB;
GO

IF OBJECT_ID('dbo.Verifications', 'U') IS NULL
BEGIN
    CREATE TABLE Verifications (
        VerificationId INT IDENTITY(1,1) PRIMARY KEY,
        SupplyRequestId INT NOT NULL,
        ItemName NVARCHAR(100) NOT NULL,
        OfficerName NVARCHAR(100) NOT NULL,
        Decision NVARCHAR(30) NOT NULL,
        Remarks NVARCHAR(250) NULL,
        VerifiedAt DATETIME NOT NULL DEFAULT GETDATE()
    );

    INSERT INTO Verifications (SupplyRequestId, ItemName, OfficerName, Decision, Remarks) VALUES
        (1, N'Urea Fertilizer', N'Dr. Chaminda Silva', N'Approved', N'Approved for agricultural use.');
END
GO

IF OBJECT_ID('dbo.Advisories', 'U') IS NULL
BEGIN
    CREATE TABLE Advisories (
        AdvisoryId INT IDENTITY(1,1) PRIMARY KEY,
        Title NVARCHAR(150) NOT NULL,
        District NVARCHAR(80) NOT NULL,
        CropType NVARCHAR(80) NOT NULL,
        Message NVARCHAR(500) NOT NULL,
        OfficerName NVARCHAR(100) NOT NULL,
        PublishedAt DATETIME NOT NULL DEFAULT GETDATE()
    );

    INSERT INTO Advisories (Title, District, CropType, Message, OfficerName) VALUES
        (N'Monsoon Paddy Care', N'All Districts', N'Paddy', N'Ensure proper field drainage before the upcoming monsoon season to prevent waterlogging.', N'Dr. Chaminda Silva');
END
GO
