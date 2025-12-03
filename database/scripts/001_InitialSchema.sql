-- Historical Reporting Database - Initial Schema
-- Run this script against SQL Express to create the initial database structure

-- Create the database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'HistoricalReporting')
BEGIN
    CREATE DATABASE HistoricalReporting;
END
GO

USE HistoricalReporting;
GO

-- Users table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        Email NVARCHAR(256) NOT NULL,
        PasswordHash NVARCHAR(MAX) NOT NULL,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        LastLoginAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CreatedBy NVARCHAR(256) NULL,
        UpdatedBy NVARCHAR(256) NULL
    );

    CREATE UNIQUE INDEX IX_Users_Email ON Users(Email);
END
GO

-- Reports table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Reports')
BEGIN
    CREATE TABLE Reports (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        Query NVARCHAR(MAX) NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Draft',
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        CreatedBy NVARCHAR(256) NULL,
        UpdatedBy NVARCHAR(256) NULL
    );

    CREATE INDEX IX_Reports_Name ON Reports(Name);
    CREATE INDEX IX_Reports_Status ON Reports(Status);
END
GO

PRINT 'Initial schema created successfully.';
GO
