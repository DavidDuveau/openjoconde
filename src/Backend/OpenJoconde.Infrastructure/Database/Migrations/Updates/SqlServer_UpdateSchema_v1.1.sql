-- OpenJoconde - Migration de mise à jour du schéma pour SQL Server
-- Version: 1.1
-- Date: 2025-05-09
-- Cette migration met à jour et complète les tables existantes selon la documentation

-- 1. Modifications de champs existants pour correspondre au DbContext

-- Mettre à jour les longueurs de champs dans la table Artwork
ALTER TABLE Artwork 
    ALTER COLUMN Reference NVARCHAR(100);
    
ALTER TABLE Artwork
    ALTER COLUMN Title NVARCHAR(250);
    
ALTER TABLE Artwork
    ALTER COLUMN Description NVARCHAR(2000);

-- Mettre à jour les longueurs de champs dans la table Museum
ALTER TABLE Museum
    ALTER COLUMN Name NVARCHAR(200);

-- 2. Création d'une table manquante pour les données de synchronisation

-- Cette table permettra de suivre les synchronisations avec la source de données
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DataSyncLog')
BEGIN
    CREATE TABLE DataSyncLog (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        SyncType NVARCHAR(50) NOT NULL, -- 'Full' ou 'Incremental'
        StartedAt DATETIME2 NOT NULL,
        CompletedAt DATETIME2,
        Status NVARCHAR(20) NOT NULL, -- 'Running', 'Completed', 'Failed'
        ArtworksProcessed INT DEFAULT 0,
        ArtistsProcessed INT DEFAULT 0,
        StartTime DATETIME2 NOT NULL,
        EndTime DATETIME2,
        Success BIT NOT NULL,
        ErrorMessage NVARCHAR(MAX),
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END
GO

-- 3. Ajout d'indices supplémentaires pour améliorer les performances

-- Création d'index fulltext pour la recherche
IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'OpenJocondeCatalog')
BEGIN
    CREATE FULLTEXT CATALOG OpenJocondeCatalog AS DEFAULT;
END
GO

-- Ajout d'index fulltext sur les champs textuels pour permettre la recherche partielle
IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('Artwork'))
BEGIN
    CREATE FULLTEXT INDEX ON Artwork(Title, Description) 
    KEY INDEX PK_Artwork
    WITH STOPLIST = SYSTEM;
END
GO

IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('Artist'))
BEGIN
    CREATE FULLTEXT INDEX ON Artist(LastName, FirstName) 
    KEY INDEX PK_Artist
    WITH STOPLIST = SYSTEM;
END
GO

IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('Museum'))
BEGIN
    CREATE FULLTEXT INDEX ON Museum(Name, City) 
    KEY INDEX PK_Museum
    WITH STOPLIST = SYSTEM;
END
GO

-- 5. Ajout d'une table pour les statistiques d'utilisation
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UsageStatistics')
BEGIN
    CREATE TABLE UsageStatistics (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        EventType NVARCHAR(50) NOT NULL, -- 'Search', 'ArtworkView', etc.
        EventDate DATETIME2 NOT NULL DEFAULT GETDATE(),
        UserId NVARCHAR(100), -- Anonyme ou identifié
        Parameters NVARCHAR(MAX), -- Paramètres associés à l'événement en JSON
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END
GO

-- 6. Table pour les images associées aux œuvres
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ArtworkImage')
BEGIN
    CREATE TABLE ArtworkImage (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        ArtworkId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Artwork(Id),
        ImageUrl NVARCHAR(1000) NOT NULL,
        Caption NVARCHAR(500),
        IsPrimary BIT DEFAULT 0,
        Width INT,
        Height INT,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        IsDeleted BIT NOT NULL DEFAULT 0
    );
END
GO

-- Trigger pour mettre à jour le champ UpdatedAt
CREATE OR ALTER TRIGGER update_artwork_image_timestamp
ON ArtworkImage
AFTER UPDATE
AS
BEGIN
    UPDATE ArtworkImage
    SET UpdatedAt = GETDATE()
    FROM ArtworkImage ai
    INNER JOIN inserted i ON ai.Id = i.Id;
END;
GO

-- 7. Ajout d'une table de métadonnées pour la base Joconde
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JocondeMetadata')
BEGIN
    CREATE TABLE JocondeMetadata (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        SourceVersion NVARCHAR(100),
        LastUpdateDate DATETIME2,
        TotalRecords INT,
        SchemaVersion NVARCHAR(50),
        Notes NVARCHAR(MAX),
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END
GO

-- Trigger pour mettre à jour le champ UpdatedAt
CREATE OR ALTER TRIGGER update_joconde_metadata_timestamp
ON JocondeMetadata
AFTER UPDATE
AS
BEGIN
    UPDATE JocondeMetadata
    SET UpdatedAt = GETDATE()
    FROM JocondeMetadata jm
    INNER JOIN inserted i ON jm.Id = i.Id;
END;
GO

-- 8. Ajout d'une table pour les mots-clés/tags
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tag')
BEGIN
    CREATE TABLE Tag (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Name NVARCHAR(100) NOT NULL UNIQUE,
        Category NVARCHAR(50),
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        IsDeleted BIT NOT NULL DEFAULT 0
    );
END
GO

-- Trigger pour mettre à jour le champ UpdatedAt
CREATE OR ALTER TRIGGER update_tag_timestamp
ON Tag
AFTER UPDATE
AS
BEGIN
    UPDATE Tag
    SET UpdatedAt = GETDATE()
    FROM Tag t
    INNER JOIN inserted i ON t.Id = i.Id;
END;
GO

-- 9. Table de relation entre œuvres et tags
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ArtworkTag')
BEGIN
    CREATE TABLE ArtworkTag (
        ArtworkId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Artwork(Id),
        TagId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Tag(Id),
        PRIMARY KEY (ArtworkId, TagId)
    );
END
GO

-- Ajout d'un index pour améliorer les performances
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'idx_artwork_tag_tag' AND object_id = OBJECT_ID('ArtworkTag'))
BEGIN
    CREATE INDEX idx_artwork_tag_tag ON ArtworkTag(TagId);
END
GO
