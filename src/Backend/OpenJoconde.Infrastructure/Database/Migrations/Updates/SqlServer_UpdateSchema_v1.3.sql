-- Migration: Update all entity column lengths to support longer data from Joconde
-- Version: 1.3
-- Date: 2025-07-03
-- Description: Increase column lengths across all entities to accommodate long Joconde data and prevent truncation errors

-- Update Artist table columns
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Artist') AND name = 'LastName')
BEGIN
    ALTER TABLE Artist ALTER COLUMN LastName NVARCHAR(500) NOT NULL;
    PRINT 'Successfully updated Artist.LastName column to NVARCHAR(500)';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Artist') AND name = 'FirstName')
BEGIN
    ALTER TABLE Artist ALTER COLUMN FirstName NVARCHAR(500);
    PRINT 'Successfully updated Artist.FirstName column to NVARCHAR(500)';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Artist') AND name = 'BirthDate')
BEGIN
    ALTER TABLE Artist ALTER COLUMN BirthDate NVARCHAR(100);
    PRINT 'Successfully updated Artist.BirthDate column to NVARCHAR(100)';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Artist') AND name = 'DeathDate')
BEGIN
    ALTER TABLE Artist ALTER COLUMN DeathDate NVARCHAR(100);
    PRINT 'Successfully updated Artist.DeathDate column to NVARCHAR(100)';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Artist') AND name = 'Nationality')
BEGIN
    ALTER TABLE Artist ALTER COLUMN Nationality NVARCHAR(200);
    PRINT 'Successfully updated Artist.Nationality column to NVARCHAR(200)';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Artist') AND name = 'Biography')
BEGIN
    ALTER TABLE Artist ALTER COLUMN Biography NVARCHAR(MAX);
    PRINT 'Successfully updated Artist.Biography column to NVARCHAR(MAX)';
END

-- Update Domain table columns
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Domain') AND name = 'Name')
BEGIN
    ALTER TABLE Domain ALTER COLUMN Name NVARCHAR(200) NOT NULL;
    PRINT 'Successfully updated Domain.Name column to NVARCHAR(200)';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Domain') AND name = 'Description')
BEGIN
    ALTER TABLE Domain ALTER COLUMN Description NVARCHAR(MAX);
    PRINT 'Successfully updated Domain.Description column to NVARCHAR(MAX)';
END

-- Update Technique table columns
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Technique') AND name = 'Name')
BEGIN
    ALTER TABLE Technique ALTER COLUMN Name NVARCHAR(200) NOT NULL;
    PRINT 'Successfully updated Technique.Name column to NVARCHAR(200)';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Technique') AND name = 'Description')
BEGIN
    ALTER TABLE Technique ALTER COLUMN Description NVARCHAR(MAX);
    PRINT 'Successfully updated Technique.Description column to NVARCHAR(MAX)';
END

-- Update Period table columns
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Period') AND name = 'Name')
BEGIN
    ALTER TABLE Period ALTER COLUMN Name NVARCHAR(200) NOT NULL;
    PRINT 'Successfully updated Period.Name column to NVARCHAR(200)';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Period') AND name = 'Description')
BEGIN
    ALTER TABLE Period ALTER COLUMN Description NVARCHAR(MAX);
    PRINT 'Successfully updated Period.Description column to NVARCHAR(MAX)';
END

-- Update Museum table columns
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Museum') AND name = 'Name')
BEGIN
    ALTER TABLE Museum ALTER COLUMN Name NVARCHAR(500) NOT NULL;
    PRINT 'Successfully updated Museum.Name column to NVARCHAR(500)';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Museum') AND name = 'City')
BEGIN
    ALTER TABLE Museum ALTER COLUMN City NVARCHAR(200);
    PRINT 'Successfully updated Museum.City column to NVARCHAR(200)';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Museum') AND name = 'Department')
BEGIN
    ALTER TABLE Museum ALTER COLUMN Department NVARCHAR(200);
    PRINT 'Successfully updated Museum.Department column to NVARCHAR(200)';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Museum') AND name = 'Phone')
BEGIN
    ALTER TABLE Museum ALTER COLUMN Phone NVARCHAR(50);
    PRINT 'Successfully updated Museum.Phone column to NVARCHAR(50)';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Museum') AND name = 'Email')
BEGIN
    ALTER TABLE Museum ALTER COLUMN Email NVARCHAR(200);
    PRINT 'Successfully updated Museum.Email column to NVARCHAR(200)';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Museum') AND name = 'Website')
BEGIN
    ALTER TABLE Museum ALTER COLUMN Website NVARCHAR(500);
    PRINT 'Successfully updated Museum.Website column to NVARCHAR(500)';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Museum') AND name = 'Address')
BEGIN
    ALTER TABLE Museum ALTER COLUMN Address NVARCHAR(MAX);
    PRINT 'Successfully updated Museum.Address column to NVARCHAR(MAX)';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Museum') AND name = 'Description')
BEGIN
    ALTER TABLE Museum ALTER COLUMN Description NVARCHAR(MAX);
    PRINT 'Successfully updated Museum.Description column to NVARCHAR(MAX)';
END

-- Update statistics for better query performance
UPDATE STATISTICS Artist;
UPDATE STATISTICS Domain;
UPDATE STATISTICS Technique;
UPDATE STATISTICS Period;
UPDATE STATISTICS Museum;

PRINT 'Migration v1.3 completed successfully - All entity columns updated for longer Joconde data';