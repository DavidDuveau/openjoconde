-- Migration: Update Title column length to support longer artwork titles
-- Version: 1.2
-- Date: 2025-07-03
-- Description: Increase Title column in Artwork table from 250 to 500 characters to accommodate long Joconde artwork titles

-- Check if the Title column exists before altering it
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Artwork') AND name = 'Title')
BEGIN
    -- Update the Title column to allow longer text
    ALTER TABLE Artwork ALTER COLUMN Title NVARCHAR(1000);
    PRINT 'Successfully updated Artwork.Title column to NVARCHAR(1000)';
END
ELSE
BEGIN
    PRINT 'Artwork.Title column not found - skipping Title migration';
END

-- Check if the Description column exists before altering it
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Artwork') AND name = 'Description')
BEGIN
    -- Update the Description column to allow unlimited text
    ALTER TABLE Artwork ALTER COLUMN Description NVARCHAR(MAX);
    PRINT 'Successfully updated Artwork.Description column to NVARCHAR(MAX)';
END
ELSE
BEGIN
    PRINT 'Artwork.Description column not found - skipping Description migration';
END

-- Check if the InventoryNumber column exists before altering it
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Artwork') AND name = 'InventoryNumber')
BEGIN
    -- Update the InventoryNumber column to allow longer values
    ALTER TABLE Artwork ALTER COLUMN InventoryNumber NVARCHAR(500);
    PRINT 'Successfully updated Artwork.InventoryNumber column to NVARCHAR(500)';
END
ELSE
BEGIN
    PRINT 'Artwork.InventoryNumber column not found - skipping InventoryNumber migration';
END

-- Update statistics for better query performance
UPDATE STATISTICS Artwork;

PRINT 'Migration v1.2 completed successfully';