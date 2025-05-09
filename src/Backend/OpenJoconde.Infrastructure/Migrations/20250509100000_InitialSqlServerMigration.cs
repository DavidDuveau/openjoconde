using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace OpenJoconde.Infrastructure.Migrations
{
    public partial class InitialSqlServerMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Pour cette migration initiale, nous utilisons le script SQL directement
            // car il contient des instructions spécifiques à SQL Server
            migrationBuilder.Sql(@"
-- Ce code SQL est inséré par la migration
-- OpenJoconde - Migration initiale pour SQL Server
-- Les tables sont créées automatiquement par EF Core
-- Ce script ajoute les triggers et données initiales

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

-- Insertion de quelques domaines artistiques de base (pour tests)
IF NOT EXISTS (SELECT TOP 1 1 FROM Domain)
BEGIN
    INSERT INTO Domain (Id, Name, Description, CreatedAt, UpdatedAt) VALUES
    (NEWID(), 'Peinture', 'Œuvres picturales réalisées sur différents supports', GETDATE(), GETDATE()),
    (NEWID(), 'Sculpture', 'Œuvres en trois dimensions sculptées dans divers matériaux', GETDATE(), GETDATE()),
    (NEWID(), 'Arts graphiques', 'Dessins, estampes, gravures et autres œuvres sur papier', GETDATE(), GETDATE()),
    (NEWID(), 'Mobilier', 'Meubles et objets d''ameublement', GETDATE(), GETDATE()),
    (NEWID(), 'Photographie', 'Images produites par des procédés photographiques', GETDATE(), GETDATE());
END

-- Insertion de quelques techniques de base (pour tests)
IF NOT EXISTS (SELECT TOP 1 1 FROM Technique)
BEGIN
    INSERT INTO Technique (Id, Name, Description, CreatedAt, UpdatedAt) VALUES
    (NEWID(), 'Huile sur toile', 'Peinture à l''huile sur support en toile', GETDATE(), GETDATE()),
    (NEWID(), 'Aquarelle', 'Peinture à l''eau sur papier', GETDATE(), GETDATE()),
    (NEWID(), 'Marbre', 'Sculpture sur marbre', GETDATE(), GETDATE()),
    (NEWID(), 'Bronze', 'Sculpture en bronze coulé', GETDATE(), GETDATE()),
    (NEWID(), 'Gravure', 'Impression à partir d''une matrice gravée', GETDATE(), GETDATE());
END

-- Insertion de quelques périodes artistiques (pour tests)
IF NOT EXISTS (SELECT TOP 1 1 FROM Period)
BEGIN
    INSERT INTO Period (Id, Name, StartYear, EndYear, Description, CreatedAt, UpdatedAt) VALUES
    (NEWID(), 'Renaissance', 1400, 1600, 'Période artistique marquée par un renouveau des arts et de la culture en Europe', GETDATE(), GETDATE()),
    (NEWID(), 'Baroque', 1600, 1750, 'Style artistique caractérisé par l''exubérance et le mouvement', GETDATE(), GETDATE()),
    (NEWID(), 'Classicisme', 1650, 1750, 'Mouvement artistique cherchant à imiter l''art antique', GETDATE(), GETDATE()),
    (NEWID(), 'Romantisme', 1770, 1850, 'Mouvement artistique valorisant les émotions et l''imagination', GETDATE(), GETDATE()),
    (NEWID(), 'Impressionnisme', 1860, 1900, 'Mouvement pictural caractérisé par la peinture en plein air et la captation des impressions', GETDATE(), GETDATE());
END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Pour révoquer cette migration, on supprime les triggers
            migrationBuilder.Sql(@"
DROP TRIGGER IF EXISTS update_artwork_timestamp;
DROP TRIGGER IF EXISTS update_artist_timestamp;
DROP TRIGGER IF EXISTS update_domain_timestamp;
DROP TRIGGER IF EXISTS update_technique_timestamp;
DROP TRIGGER IF EXISTS update_period_timestamp;
DROP TRIGGER IF EXISTS update_museum_timestamp;
            ");
        }
    }
}
