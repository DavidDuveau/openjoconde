-- OpenJoconde - Script initial de création de la base de données
-- Basé sur les recommandations de la charte de développement du projet

-- Suppression des tables existantes (si nécessaire)
DROP TABLE IF EXISTS ArtworkPeriod;
DROP TABLE IF EXISTS ArtworkTechnique;
DROP TABLE IF EXISTS ArtworkDomain;
DROP TABLE IF EXISTS ArtworkArtist;
DROP TABLE IF EXISTS Artwork;
DROP TABLE IF EXISTS Artist;
DROP TABLE IF EXISTS Museum;
DROP TABLE IF EXISTS Period;
DROP TABLE IF EXISTS Technique;
DROP TABLE IF EXISTS Domain;

-- Création des tables principales

-- Table Domain (Domaine)
CREATE TABLE Domain (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(100) NOT NULL UNIQUE,
    Description TEXT,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsDeleted BOOLEAN NOT NULL DEFAULT FALSE
);

-- Table Technique
CREATE TABLE Technique (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(100) NOT NULL UNIQUE,
    Description TEXT,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsDeleted BOOLEAN NOT NULL DEFAULT FALSE
);

-- Table Period (Epoque)
CREATE TABLE Period (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(100) NOT NULL UNIQUE,
    StartYear INT,
    EndYear INT,
    Description TEXT,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsDeleted BOOLEAN NOT NULL DEFAULT FALSE
);

-- Table Museum (Musée)
CREATE TABLE Museum (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(255) NOT NULL,
    City VARCHAR(100),
    Department VARCHAR(100),
    Address VARCHAR(255),
    ZipCode VARCHAR(20),
    Phone VARCHAR(50),
    Email VARCHAR(100),
    Website VARCHAR(255),
    Description TEXT,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsDeleted BOOLEAN NOT NULL DEFAULT FALSE
);

-- Table Artist (Artiste)
CREATE TABLE Artist (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    LastName VARCHAR(100) NOT NULL,
    FirstName VARCHAR(100),
    BirthDate VARCHAR(100),    -- Format flexible pour dates historiques
    DeathDate VARCHAR(100),    -- Format flexible pour dates historiques
    Nationality VARCHAR(100),
    Biography TEXT,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsDeleted BOOLEAN NOT NULL DEFAULT FALSE
);

-- Table Artwork (Œuvre)
CREATE TABLE Artwork (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Reference VARCHAR(50) UNIQUE,  -- REF dans le XML
    InventoryNumber VARCHAR(100),  -- INV dans le XML
    Denomination VARCHAR(255),
    Title VARCHAR(500),
    Description TEXT,
    Dimensions VARCHAR(255),
    CreationDate VARCHAR(100),     -- Format flexible pour dates historiques
    CreationPlace VARCHAR(255),
    MuseumId UUID REFERENCES Museum(Id),
    Copyright VARCHAR(255),
    ImageUrl VARCHAR(1000),
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsDeleted BOOLEAN NOT NULL DEFAULT FALSE
);

-- Tables de relations

-- Relation entre Artwork et Artist
CREATE TABLE ArtworkArtist (
    ArtworkId UUID REFERENCES Artwork(Id),
    ArtistId UUID REFERENCES Artist(Id),
    Role VARCHAR(100),
    PRIMARY KEY (ArtworkId, ArtistId)
);

-- Relation entre Artwork et Domain
CREATE TABLE ArtworkDomain (
    ArtworkId UUID REFERENCES Artwork(Id),
    DomainId UUID REFERENCES Domain(Id),
    PRIMARY KEY (ArtworkId, DomainId)
);

-- Relation entre Artwork et Technique
CREATE TABLE ArtworkTechnique (
    ArtworkId UUID REFERENCES Artwork(Id),
    TechniqueId UUID REFERENCES Technique(Id),
    PRIMARY KEY (ArtworkId, TechniqueId)
);

-- Relation entre Artwork et Period
CREATE TABLE ArtworkPeriod (
    ArtworkId UUID REFERENCES Artwork(Id),
    PeriodId UUID REFERENCES Period(Id),
    PRIMARY KEY (ArtworkId, PeriodId)
);

-- Création des index pour optimiser les performances

-- Index sur Artwork
CREATE INDEX idx_artwork_reference ON Artwork(Reference);
CREATE INDEX idx_artwork_inventory_number ON Artwork(InventoryNumber);
CREATE INDEX idx_artwork_title ON Artwork(Title);
CREATE INDEX idx_artwork_denomination ON Artwork(Denomination);
CREATE INDEX idx_artwork_museum ON Artwork(MuseumId);

-- Index sur Artist
CREATE INDEX idx_artist_lastname ON Artist(LastName);
CREATE INDEX idx_artist_nationality ON Artist(Nationality);

-- Index sur Museum
CREATE INDEX idx_museum_name ON Museum(Name);
CREATE INDEX idx_museum_city ON Museum(City);
CREATE INDEX idx_museum_department ON Museum(Department);

-- Index sur Period
CREATE INDEX idx_period_name ON Period(Name);
CREATE INDEX idx_period_years ON Period(StartYear, EndYear);

-- Index sur les tables de relations
CREATE INDEX idx_artwork_artist_artist ON ArtworkArtist(ArtistId);
CREATE INDEX idx_artwork_domain_domain ON ArtworkDomain(DomainId);
CREATE INDEX idx_artwork_technique_technique ON ArtworkTechnique(TechniqueId);
CREATE INDEX idx_artwork_period_period ON ArtworkPeriod(PeriodId);

-- Commentaires de table
COMMENT ON TABLE Domain IS 'Domaines artistiques et catégories d''œuvres';
COMMENT ON TABLE Technique IS 'Techniques artistiques utilisées';
COMMENT ON TABLE Period IS 'Périodes et époques historiques';
COMMENT ON TABLE Museum IS 'Musées conservant les œuvres';
COMMENT ON TABLE Artist IS 'Artistes et créateurs des œuvres';
COMMENT ON TABLE Artwork IS 'Œuvres d''art et objets patrimoniaux';
COMMENT ON TABLE ArtworkArtist IS 'Relation entre œuvres et artistes';
COMMENT ON TABLE ArtworkDomain IS 'Relation entre œuvres et domaines';
COMMENT ON TABLE ArtworkTechnique IS 'Relation entre œuvres et techniques';
COMMENT ON TABLE ArtworkPeriod IS 'Relation entre œuvres et périodes';
