-- OpenJoconde - Migration initiale
-- Ce script est conçu pour être compatible avec les migrations Entity Framework Core
-- Il peut être exécuté manuellement ou via le mécanisme de migration d'EF Core

-- Version: 1.0
-- Date: 2025-03-22

-- Fonction pour tracking des mises à jour
CREATE OR REPLACE FUNCTION update_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.UpdatedAt = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Triggers pour mettre à jour automatiquement le champ UpdatedAt
CREATE TRIGGER update_artwork_timestamp
BEFORE UPDATE ON Artwork
FOR EACH ROW
EXECUTE FUNCTION update_timestamp();

CREATE TRIGGER update_artist_timestamp
BEFORE UPDATE ON Artist
FOR EACH ROW
EXECUTE FUNCTION update_timestamp();

CREATE TRIGGER update_domain_timestamp
BEFORE UPDATE ON Domain
FOR EACH ROW
EXECUTE FUNCTION update_timestamp();

CREATE TRIGGER update_technique_timestamp
BEFORE UPDATE ON Technique
FOR EACH ROW
EXECUTE FUNCTION update_timestamp();

CREATE TRIGGER update_period_timestamp
BEFORE UPDATE ON Period
FOR EACH ROW
EXECUTE FUNCTION update_timestamp();

CREATE TRIGGER update_museum_timestamp
BEFORE UPDATE ON Museum
FOR EACH ROW
EXECUTE FUNCTION update_timestamp();

-- Insertion de quelques domaines artistiques de base (pour tests)
INSERT INTO Domain (Name, Description) VALUES
('Peinture', 'Œuvres picturales réalisées sur différents supports'),
('Sculpture', 'Œuvres en trois dimensions sculptées dans divers matériaux'),
('Arts graphiques', 'Dessins, estampes, gravures et autres œuvres sur papier'),
('Mobilier', 'Meubles et objets d''ameublement'),
('Photographie', 'Images produites par des procédés photographiques');

-- Insertion de quelques techniques de base (pour tests)
INSERT INTO Technique (Name, Description) VALUES
('Huile sur toile', 'Peinture à l''huile sur support en toile'),
('Aquarelle', 'Peinture à l''eau sur papier'),
('Marbre', 'Sculpture sur marbre'),
('Bronze', 'Sculpture en bronze coulé'),
('Gravure', 'Impression à partir d''une matrice gravée');

-- Insertion de quelques périodes artistiques (pour tests)
INSERT INTO Period (Name, StartYear, EndYear, Description) VALUES
('Renaissance', 1400, 1600, 'Période artistique marquée par un renouveau des arts et de la culture en Europe'),
('Baroque', 1600, 1750, 'Style artistique caractérisé par l''exubérance et le mouvement'),
('Classicisme', 1650, 1750, 'Mouvement artistique cherchant à imiter l''art antique'),
('Romantisme', 1770, 1850, 'Mouvement artistique valorisant les émotions et l''imagination'),
('Impressionnisme', 1860, 1900, 'Mouvement pictural caractérisé par la peinture en plein air et la captation des impressions');