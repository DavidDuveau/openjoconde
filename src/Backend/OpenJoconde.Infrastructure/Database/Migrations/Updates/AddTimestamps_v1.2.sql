-- Migration pour ajouter les colonnes CreatedAt et UpdatedAt aux tables qui ne les ont pas

-- Ajouter CreatedAt à la table Artwork si elle n'existe pas
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Artwork]') AND name = 'CreatedAt')
BEGIN
    ALTER TABLE [dbo].[Artwork] ADD [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE();
END

-- Ajouter CreatedAt et UpdatedAt à la table Artist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Artist]') AND name = 'CreatedAt')
BEGIN
    ALTER TABLE [dbo].[Artist] ADD [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE();
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Artist]') AND name = 'UpdatedAt')
BEGIN
    ALTER TABLE [dbo].[Artist] ADD [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE();
END

-- Ajouter CreatedAt et UpdatedAt à la table Domain
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Domain]') AND name = 'CreatedAt')
BEGIN
    ALTER TABLE [dbo].[Domain] ADD [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE();
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Domain]') AND name = 'UpdatedAt')
BEGIN
    ALTER TABLE [dbo].[Domain] ADD [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE();
END

-- Ajouter CreatedAt et UpdatedAt à la table Technique
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Technique]') AND name = 'CreatedAt')
BEGIN
    ALTER TABLE [dbo].[Technique] ADD [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE();
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Technique]') AND name = 'UpdatedAt')
BEGIN
    ALTER TABLE [dbo].[Technique] ADD [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE();
END

-- Ajouter CreatedAt et UpdatedAt à la table Period
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Period]') AND name = 'CreatedAt')
BEGIN
    ALTER TABLE [dbo].[Period] ADD [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE();
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Period]') AND name = 'UpdatedAt')
BEGIN
    ALTER TABLE [dbo].[Period] ADD [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE();
END

-- Ajouter CreatedAt et UpdatedAt à la table Museum
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Museum]') AND name = 'CreatedAt')
BEGIN
    ALTER TABLE [dbo].[Museum] ADD [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE();
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Museum]') AND name = 'UpdatedAt')
BEGIN
    ALTER TABLE [dbo].[Museum] ADD [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE();
END

-- Mettre à jour les valeurs existantes avec la date actuelle
UPDATE [dbo].[Artwork] SET [CreatedAt] = GETUTCDATE() WHERE [CreatedAt] IS NULL;
UPDATE [dbo].[Artist] SET [CreatedAt] = GETUTCDATE(), [UpdatedAt] = GETUTCDATE() WHERE [CreatedAt] IS NULL OR [UpdatedAt] IS NULL;
UPDATE [dbo].[Domain] SET [CreatedAt] = GETUTCDATE(), [UpdatedAt] = GETUTCDATE() WHERE [CreatedAt] IS NULL OR [UpdatedAt] IS NULL;
UPDATE [dbo].[Technique] SET [CreatedAt] = GETUTCDATE(), [UpdatedAt] = GETUTCDATE() WHERE [CreatedAt] IS NULL OR [UpdatedAt] IS NULL;
UPDATE [dbo].[Period] SET [CreatedAt] = GETUTCDATE(), [UpdatedAt] = GETUTCDATE() WHERE [CreatedAt] IS NULL OR [UpdatedAt] IS NULL;
UPDATE [dbo].[Museum] SET [CreatedAt] = GETUTCDATE(), [UpdatedAt] = GETUTCDATE() WHERE [CreatedAt] IS NULL OR [UpdatedAt] IS NULL;
