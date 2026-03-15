-- =============================================================================
-- ADO.NET Learning Guide — Sample Database Setup
-- Run this script once against your SQL Server instance before running C# examples.
-- Compatible with SQL Server 2016+ / Azure SQL Database.
-- =============================================================================

USE master;
GO

-- Create database if not exists (drop and recreate for clean lab; comment out in production)
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'AdoNetSample')
    DROP DATABASE AdoNetSample;
GO
CREATE DATABASE AdoNetSample;
GO

USE AdoNetSample;
GO

-- =============================================================================
-- Tables
-- =============================================================================

-- Customers: used for CRUD, DataReader, DataAdapter, parameters, filtering
CREATE TABLE dbo.Customers (
    Id           INT IDENTITY(1,1) NOT NULL,
    Name         NVARCHAR(100)    NOT NULL,
    Email        NVARCHAR(255)    NULL,
    City         NVARCHAR(100)    NULL,
    Country      NVARCHAR(100)    NULL,
    CreatedAt    DATETIME2(0)     NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Customers PRIMARY KEY CLUSTERED (Id)
);
CREATE NONCLUSTERED INDEX IX_Customers_City ON dbo.Customers (City);
CREATE NONCLUSTERED INDEX IX_Customers_Country ON dbo.Customers (Country);

-- Orders: for multi-table DataSet, relations, multiple result sets
CREATE TABLE dbo.Orders (
    Id           INT IDENTITY(1,1) NOT NULL,
    CustomerId   INT              NOT NULL,
    OrderNumber  NVARCHAR(50)     NOT NULL,
    OrderDate    DATE             NOT NULL,
    TotalAmount  DECIMAL(18,2)    NOT NULL,
    Status       NVARCHAR(20)     NOT NULL CONSTRAINT DF_Orders_Status DEFAULT (N'Pending'),
    CONSTRAINT PK_Orders PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id)
);
CREATE NONCLUSTERED INDEX IX_Orders_CustomerId ON dbo.Orders (CustomerId);
CREATE NONCLUSTERED INDEX IX_Orders_OrderDate ON dbo.Orders (OrderDate);

-- Accounts: for transaction examples (transfer balance)
CREATE TABLE dbo.Accounts (
    Id           INT              NOT NULL,
    AccountName  NVARCHAR(100)    NOT NULL,
    Balance      DECIMAL(18,2)    NOT NULL CONSTRAINT DF_Accounts_Balance DEFAULT (0),
    CONSTRAINT PK_Accounts PRIMARY KEY CLUSTERED (Id)
);

-- Files: for BLOB / streaming large data examples (optional)
CREATE TABLE dbo.Files (
    Id           INT IDENTITY(1,1) NOT NULL,
    FileName     NVARCHAR(255)    NOT NULL,
    ContentType  NVARCHAR(100)    NULL,
    Content      VARBINARY(MAX)   NULL,
    CreatedAt    DATETIME2(0)     NOT NULL CONSTRAINT DF_Files_CreatedAt DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Files PRIMARY KEY CLUSTERED (Id)
);

-- ApplicationUsers: for SQL injection / parameterized query examples
CREATE TABLE dbo.ApplicationUsers (
    Id           INT IDENTITY(1,1) NOT NULL,
    UserName     NVARCHAR(50)     NOT NULL,
    DisplayName  NVARCHAR(100)    NULL,
    IsActive     BIT              NOT NULL CONSTRAINT DF_ApplicationUsers_IsActive DEFAULT (1),
    CONSTRAINT PK_ApplicationUsers PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_ApplicationUsers_UserName UNIQUE (UserName)
);

-- =============================================================================
-- Seed Data
-- =============================================================================

INSERT INTO dbo.Customers (Name, Email, City, Country) VALUES
    (N'Contoso Ltd',        N'contact@contoso.com',    N'London',    N'United Kingdom'),
    (N'Fabrikam Inc',       N'info@fabrikam.com',      N'Seattle',   N'USA'),
    (N'Adventure Works',    N'sales@adventureworks.com', N'Sydney', N'Australia'),
    (N'Tailspin Toys',      N'support@tailspin.com',   N'Paris',     N'France'),
    (N'Northwind Traders',  N'orders@northwind.com',   N'London',    N'United Kingdom');

INSERT INTO dbo.Orders (CustomerId, OrderNumber, OrderDate, TotalAmount, Status) VALUES
    (1, N'ORD-2024-001', '2024-01-15', 1250.00, N'Shipped'),
    (1, N'ORD-2024-002', '2024-02-20',  890.50, N'Pending'),
    (2, N'ORD-2024-003', '2024-03-10', 2100.00, N'Shipped'),
    (3, N'ORD-2024-004', '2024-03-22',  456.75, N'Processing'),
    (4, N'ORD-2024-005', '2024-04-01', 1780.00, N'Pending');

INSERT INTO dbo.Accounts (Id, AccountName, Balance) VALUES
    (1, N'Operating Account',  10000.00),
    (2, N'Reserve Account',     5000.00);

INSERT INTO dbo.ApplicationUsers (UserName, DisplayName, IsActive) VALUES
    (N'admin',   N'Administrator', 1),
    (N'jdoe',    N'John Doe',      1),
    (N'jsmith',  N'Jane Smith',    1);

-- =============================================================================
-- Stored Procedures (for advanced examples)
-- =============================================================================

-- Get customer by ID (single result set, input parameter)
CREATE OR ALTER PROCEDURE dbo.usp_GetCustomerById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, Email, City, Country, CreatedAt
    FROM dbo.Customers
    WHERE Id = @Id;
END;
GO

-- Dashboard: multiple result sets (customers + orders)
CREATE OR ALTER PROCEDURE dbo.usp_GetDashboardData
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 10 Id, Name, City, Country FROM dbo.Customers ORDER BY CreatedAt DESC;
    SELECT TOP 10 Id, CustomerId, OrderNumber, OrderDate, TotalAmount, Status FROM dbo.Orders ORDER BY OrderDate DESC;
END;
GO

-- Get customer count by city (output parameter)
CREATE OR ALTER PROCEDURE dbo.usp_GetCustomerCountByCity
    @City     NVARCHAR(100),
    @Count    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT @Count = COUNT(*) FROM dbo.Customers WHERE City = @City;
END;
GO

-- Transfer balance between accounts (transaction inside SP; return 0 = success, -1 = failure)
CREATE OR ALTER PROCEDURE dbo.usp_TransferBalance
    @FromAccountId INT,
    @ToAccountId   INT,
    @Amount        DECIMAL(18,2),
    @ErrorMessage  NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @ErrorMessage = NULL;

    IF @Amount <= 0
    BEGIN
        SET @ErrorMessage = N'Amount must be positive.';
        RETURN -1;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.Accounts SET Balance = Balance - @Amount WHERE Id = @FromAccountId;
        IF @@ROWCOUNT = 0
        BEGIN
            SET @ErrorMessage = N'Source account not found or insufficient balance.';
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        UPDATE dbo.Accounts SET Balance = Balance + @Amount WHERE Id = @ToAccountId;
        IF @@ROWCOUNT = 0
        BEGIN
            SET @ErrorMessage = N'Target account not found.';
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @ErrorMessage = ERROR_MESSAGE();
        RETURN -1;
    END CATCH
END;
GO

-- Table-valued parameter: type for list of IDs
CREATE TYPE dbo.IdList AS TABLE (Id INT NOT NULL PRIMARY KEY);
GO

-- Process multiple customer IDs (TVP example)
CREATE OR ALTER PROCEDURE dbo.usp_GetCustomersByIds
    @Ids dbo.IdList READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.Id, c.Name, c.Email, c.City, c.Country
    FROM dbo.Customers c
    INNER JOIN @Ids i ON c.Id = i.Id;
END;
GO

-- =============================================================================
-- Verification
-- =============================================================================
SELECT N'Customers' AS [Table], COUNT(*) AS [Rows] FROM dbo.Customers
UNION ALL
SELECT N'Orders',    COUNT(*) FROM dbo.Orders
UNION ALL
SELECT N'Accounts',  COUNT(*) FROM dbo.Accounts
UNION ALL
SELECT N'ApplicationUsers', COUNT(*) FROM dbo.ApplicationUsers;

PRINT N'Sample database setup complete. Use connection: Server=.;Database=AdoNetSample;Integrated Security=True;TrustServerCertificate=True;';
