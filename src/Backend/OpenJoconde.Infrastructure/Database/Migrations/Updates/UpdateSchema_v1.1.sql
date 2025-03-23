-- OpenJoconde - Migration de mise à jour du schéma
-- Version: 1.1
-- Date: 2025-03-23
-- Cette migration met à jour et complète les tables existantes selon la documentation

-- IMPORTANT: Créer d'abord l'extension pg_trgm avant de créer les index qui en dépendent
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- 1. Modifications de champs existants pour correspondre au DbContext

-- Mettre à jour les longueurs de champs dans la table Artwork
ALTER TABLE Artwork 
    ALTER COLUMN Reference TYPE VARCHAR(100),
    ALTER COLUMN Title TYPE VARCHAR(250),
    ALTER COLUMN Description TYPE VARCHAR(2000);

-- Mettre à jour les longueurs de champs dans la table Museum
ALTER TABLE Museum
    ALTER COLUMN Name TYPE VARCHAR(200);

-- 2. Création d'une table manquante pour les données de synchronisation

-- Cette table permettra de suivre les synchronisations avec la source de données
CREATE TABLE IF NOT EXISTS DataSyncLog (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    SyncType VARCHAR(50) NOT NULL, -- 'Full' ou 'Incremental'
    StartedAt TIMESTAMP NOT NULL,
    CompletedAt TIMESTAMP,
    Status VARCHAR(20) NOT NULL, -- 'Running', 'Completed', 'Failed'
    ItemsProcessed INT DEFAULT 0,
    ErrorMessage TEXT,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 3. Ajout d'indices supplémentaires pour améliorer les performances

-- Amélioration des indices sur les champs textuels pour permettre la recherche partielle
CREATE INDEX IF NOT EXISTS idx_artwork_title_gin ON Artwork USING gin(Title gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_artist_lastname_gin ON Artist USING gin(LastName gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_museum_name_gin ON Museum USING gin(Name gin_trgm_ops);

-- 5. Ajout d'une table pour les statistiques d'utilisation
CREATE TABLE IF NOT EXISTS UsageStatistics (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    EventType VARCHAR(50) NOT NULL, -- 'Search', 'ArtworkView', etc.
    EventDate TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UserId VARCHAR(100), -- Anonyme ou identifié
    Parameters JSONB, -- Paramètres associés à l'événement
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 6. Table pour les images associées aux œuvres
CREATE TABLE IF NOT EXISTS ArtworkImage (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ArtworkId UUID NOT NULL REFERENCES Artwork(Id),
    ImageUrl VARCHAR(1000) NOT NULL,
    Caption VARCHAR(500),
    IsPrimary BOOLEAN DEFAULT FALSE,
    Width INT,
    Height INT,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsDeleted BOOLEAN NOT NULL DEFAULT FALSE
);

-- Trigger pour mettre à jour le champ UpdatedAt
CREATE TRIGGER update_artwork_image_timestamp
BEFORE UPDATE ON ArtworkImage
FOR EACH ROW
EXECUTE FUNCTION update_timestamp();

-- 7. Ajout d'une table de métadonnées pour la base Joconde
CREATE TABLE IF NOT EXISTS JocondeMetadata (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    SourceVersion VARCHAR(100),
    LastUpdateDate TIMESTAMP,
    TotalRecords INT,
    SchemaVersion VARCHAR(50),
    Notes TEXT,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Trigger pour mettre à jour le champ UpdatedAt
CREATE TRIGGER update_joconde_metadata_timestamp
BEFORE UPDATE ON JocondeMetadata
FOR EACH ROW
EXECUTE FUNCTION update_timestamp();

-- 8. Ajout d'une table pour les mots-clés/tags
CREATE TABLE IF NOT EXISTS Tag (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(100) NOT NULL UNIQUE,
    Category VARCHAR(50),
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsDeleted BOOLEAN NOT NULL DEFAULT FALSE
);

-- Trigger pour mettre à jour le champ UpdatedAt
CREATE TRIGGER update_tag_timestamp
BEFORE UPDATE ON Tag
FOR EACH ROW
EXECUTE FUNCTION update_timestamp();

-- 9. Table de relation entre œuvres et tags
CREATE TABLE IF NOT EXISTS ArtworkTag (
    ArtworkId UUID REFERENCES Artwork(Id),
    TagId UUID REFERENCES Tag(Id),
    PRIMARY KEY (ArtworkId, TagId)
);

-- Ajout d'un index pour améliorer les performances
CREATE INDEX IF NOT EXISTS idx_artwork_tag_tag ON ArtworkTag(TagId);

-- Commentaires de table pour les nouvelles tables
COMMENT ON TABLE DataSyncLog IS 'Journal des synchronisations de données avec la source';
COMMENT ON TABLE UsageStatistics IS 'Statistiques d''utilisation de l''application';
COMMENT ON TABLE ArtworkImage IS 'Images associées aux œuvres';
COMMENT ON TABLE JocondeMetadata IS 'Métadonnées concernant la base Joconde';
COMMENT ON TABLE Tag IS 'Mots-clés et tags pour catégoriser les œuvres';
COMMENT ON TABLE ArtworkTag IS 'Relation entre œuvres et tags';
