CREATE DATABASE AgriConnect_AdminDB;
GO
USE AgriConnect_AdminDB;
GO

CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    FullName VARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    Role VARCHAR(30) NOT NULL,
    District VARCHAR(60) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE AuditLogs (
    LogId INT IDENTITY(1,1) PRIMARY KEY,
    ServiceName VARCHAR(60) NOT NULL,
    ActionName VARCHAR(80) NOT NULL,
    Details VARCHAR(300) NOT NULL,
    LogTime DATETIME NOT NULL DEFAULT GETDATE()
);

INSERT INTO Users (FullName, Email, Role, District, IsActive) VALUES
('Nimal Perera', 'nimal@agriconnect.lk', 'Farmer', 'Anuradhapura', 1),
('Lanka Agro Supplies', 'sales@lankaagro.lk', 'Supplier', 'Kurunegala', 1),
('Dr. Chaminda Silva', 'chaminda@agri.gov.lk', 'Officer', 'All', 1),
('System Admin', 'admin@agriconnect.lk', 'Admin', 'Colombo', 1);

INSERT INTO AuditLogs (ServiceName, ActionName, Details) VALUES
('AdminWebService', 'SystemStartup', 'AgriConnect SOC platform initialized with 4 independent SQL Server databases.');
GO
