-- OpenJoconde - Migration initiale pour SQL Server
-- Ce script est conçu pour être compatible avec SQL Server et Entity Framework Core
-- Il peut être exécuté manuellement ou via le mécanisme de migration d'EF Core

-- Version: 1.0
-- Date: 2025-05-09

-- Triggers pour mettre à jour automatiquement le champ UpdatedAt
CREATE OR ALTER TRIGGER update_artwork_timestamp
ON Artwork
AFTER UPDATE
AS
BEGIN
    UPDATE Artwork
    SET UpdatedAt = GETDATE()
    FROM Artwork a
    INNER JOIN inserted i ON a.Id = i.Id;
END;
GO

CREATE OR ALTER TRIGGER update_artist_timestamp
ON Artist
AFTER UPDATE
AS
BEGIN
    UPDATE Artist
    SET UpdatedAt = GETDATE()
    FROM Artist a
    INNER JOIN inserted i ON a.Id = i.Id;
END;
GO

CREATE OR ALTER TRIGGER update_domain_timestamp
ON Domain
AFTER UPDATE
AS
BEGIN
    UPDATE Domain
    SET UpdatedAt = GETDATE()
    FROM Domain d
    INNER JOIN inserted i ON d.Id = i.Id;
END;
GO

CREATE OR ALTER TRIGGER update_technique_timestamp
ON Technique
AFTER UPDATE
AS
BEGIN
    UPDATE Technique
    SET UpdatedAt = GETDATE()
    FROM Technique t
    INNER JOIN inserted i ON t.Id = i.Id;
END;
GO

CREATE OR ALTER TRIGGER update_period_timestamp
ON Period
AFTER UPDATE
AS
BEGIN
    UPDATE Period
    SET UpdatedAt = GETDATE()
    FROM Period p
    INNER JOIN inserted i ON p.Id = i.Id;
END;
GO

CREATE OR ALTER TRIGGER update_museum_timestamp
ON Museum
AFTER UPDATE
AS
BEGIN
    UPDATE Museum
    SET UpdatedAt = GETDATE()
    FROM Museum m
    INNER JOIN inserted i ON m.Id = i.Id;
END;
GO

-- Insertion de quelques domaines artistiques de base (pour tests)
INSERT INTO Domain (Id, Name, Description, UpdatedAt, CreatedAt) VALUES
(NEWID(), 'Peinture', 'Œuvres picturales réalisées sur différents supports', GETDATE(), GETDATE()),
(NEWID(), 'Sculpture', 'Œuvres en trois dimensions sculptées dans divers matériaux', GETDATE(), GETDATE()),
(NEWID(), 'Arts graphiques', 'Dessins, estampes, gravures et autres œuvres sur papier', GETDATE(), GETDATE()),
(NEWID(), 'Mobilier', 'Meubles et objets d''ameublement', GETDATE(), GETDATE()),
(NEWID(), 'Photographie', 'Images produites par des procédés photographiques', GETDATE(), GETDATE());

-- Insertion de quelques techniques de base (pour tests)
INSERT INTO Technique (Id, Name, Description, UpdatedAt, CreatedAt) VALUES
(NEWID(), 'Huile sur toile', 'Peinture à l''huile sur support en toile', GETDATE(), GETDATE()),
(NEWID(), 'Aquarelle', 'Peinture à l''eau sur papier', GETDATE(), GETDATE()),
(NEWID(), 'Marbre', 'Sculpture sur marbre', GETDATE(), GETDATE()),
(NEWID(), 'Bronze', 'Sculpture en bronze coulé', GETDATE(), GETDATE()),
(NEWID(), 'Gravure', 'Impression à partir d''une matrice gravée', GETDATE(), GETDATE());

-- Insertion de quelques périodes artistiques (pour tests)
INSERT INTO Period (Id, Name, StartYear, EndYear, Description, UpdatedAt, CreatedAt) VALUES
(NEWID(), 'Renaissance', 1400, 1600, 'Période artistique marquée par un renouveau des arts et de la culture en Europe', GETDATE(), GETDATE()),
(NEWID(), 'Baroque', 1600, 1750, 'Style artistique caractérisé par l''exubérance et le mouvement', GETDATE(), GETDATE()),
(NEWID(), 'Classicisme', 1650, 1750, 'Mouvement artistique cherchant à imiter l''art antique', GETDATE(), GETDATE()),
(NEWID(), 'Romantisme', 1770, 1850, 'Mouvement artistique valorisant les émotions et l''imagination', GETDATE(), GETDATE()),
(NEWID(), 'Impressionnisme', 1860, 1900, 'Mouvement pictural caractérisé par la peinture en plein air et la captation des impressions', GETDATE(), GETDATE());
