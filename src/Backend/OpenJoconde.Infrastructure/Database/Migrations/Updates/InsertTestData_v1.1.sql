-- OpenJoconde - Insertion de données de test supplémentaires
-- Version: 1.1
-- Date: 2025-03-23
-- Ce script ajoute des données de test supplémentaires pour faciliter le développement

-- 1. Ajout de musées pour les tests
INSERT INTO Museum (Name, City, Department, Address, ZipCode, Phone, Website, Description)
VALUES
    ('Musée du Louvre', 'Paris', '75', 'Rue de Rivoli', '75001', '+33 1 40 20 50 50', 'https://www.louvre.fr', 'Premier musée de France et l''un des plus importants au monde.'),
    ('Musée d''Orsay', 'Paris', '75', '1 Rue de la Légion d''Honneur', '75007', '+33 1 40 49 48 14', 'https://www.musee-orsay.fr', 'Musée national présentant l''art des années 1848 à 1914.'),
    ('Centre Pompidou', 'Paris', '75', 'Place Georges-Pompidou', '75004', '+33 1 44 78 12 33', 'https://www.centrepompidou.fr', 'Musée national d''art moderne.'),
    ('Musée des Beaux-Arts de Lyon', 'Lyon', '69', '20 Place des Terreaux', '69001', '+33 4 72 10 17 40', 'https://www.mba-lyon.fr', 'Un des plus importants musées français et européens.'),
    ('Musée Fabre', 'Montpellier', '34', '39 Boulevard Bonne Nouvelle', '34000', '+33 4 67 14 83 00', 'https://museefabre.montpellier3m.fr', 'Musée d''art majeur du sud de la France.');

-- 2. Ajout d'artistes pour les tests
INSERT INTO Artist (LastName, FirstName, BirthDate, DeathDate, Nationality, Biography)
VALUES
    ('Monet', 'Claude', '1840', '1926', 'Française', 'Fondateur et figure emblématique du mouvement impressionniste.'),
    ('Picasso', 'Pablo', '1881', '1973', 'Espagnole', 'Un des plus importants artistes du XXe siècle, co-fondateur du cubisme.'),
    ('Van Gogh', 'Vincent', '1853', '1890', 'Néerlandaise', 'Figure majeure du post-impressionnisme.'),
    ('Da Vinci', 'Leonardo', '1452', '1519', 'Italienne', 'Génie universel de la Renaissance, peintre, scientifique et inventeur.'),
    ('Rodin', 'Auguste', '1840', '1917', 'Française', 'Considéré comme un des pères de la sculpture moderne.');

-- 3. Ajout d'œuvres pour les tests
INSERT INTO Artwork (Reference, InventoryNumber, Denomination, Title, Description, Dimensions, CreationDate, CreationPlace, MuseumId, Copyright, ImageUrl)
SELECT 
    'REF' || generate_series(1, 10),
    'INV' || generate_series(1, 10),
    (ARRAY['Peinture', 'Sculpture', 'Dessin', 'Gravure', 'Photographie'])[1 + floor(random() * 5)],
    (ARRAY['Paysage d''automne', 'Portrait de femme', 'Nature morte aux fruits', 'Étude de personnage', 'Vue de Paris', 'Scène mythologique', 'Abstraction n°3', 'Composition en bleu', 'Marine au couchant', 'Figures dans un jardin'])[generate_series(1, 10)],
    'Description détaillée de l''œuvre ' || generate_series(1, 10),
    (ARRAY['60 x 80 cm', '100 x 120 cm', '40 x 50 cm', '30 x 40 cm', '200 x 150 cm'])[1 + floor(random() * 5)],
    (ARRAY['XVIIe siècle', 'XVIIIe siècle', 'XIXe siècle', '1850', '1875', '1900', '1910'])[1 + floor(random() * 7)],
    (ARRAY['France', 'Italie', 'Pays-Bas', 'Espagne', 'Allemagne'])[1 + floor(random() * 5)],
    (SELECT Id FROM Museum ORDER BY random() LIMIT 1),
    'Domaine public',
    'https://example.com/images/artwork' || generate_series(1, 10) || '.jpg'
FROM generate_series(1, 10);

-- 4. Création des relations entre œuvres et artistes
INSERT INTO ArtworkArtist (ArtworkId, ArtistId, Role)
SELECT 
    a.Id,
    ar.Id,
    (ARRAY['Peintre', 'Sculpteur', 'Dessinateur', 'Graveur', 'Atelier de'])[1 + floor(random() * 5)]
FROM 
    Artwork a,
    Artist ar
WHERE random() < 0.3
LIMIT 15;

-- 5. Création des relations entre œuvres et domaines
INSERT INTO ArtworkDomain (ArtworkId, DomainId)
SELECT 
    a.Id,
    d.Id
FROM 
    Artwork a,
    Domain d
WHERE random() < 0.3
LIMIT 20;

-- 6. Création des relations entre œuvres et techniques
INSERT INTO ArtworkTechnique (ArtworkId, TechniqueId)
SELECT 
    a.Id,
    t.Id
FROM 
    Artwork a,
    Technique t
WHERE random() < 0.3
LIMIT 20;

-- 7. Création des relations entre œuvres et périodes
INSERT INTO ArtworkPeriod (ArtworkId, PeriodId)
SELECT 
    a.Id,
    p.Id
FROM 
    Artwork a,
    Period p
WHERE random() < 0.3
LIMIT 20;

-- 8. Ajout de tags
INSERT INTO Tag (Name, Category)
VALUES
    ('Portrait', 'Genre'),
    ('Paysage', 'Genre'),
    ('Nature morte', 'Genre'),
    ('Religieux', 'Thème'),
    ('Mythologique', 'Thème'),
    ('Urbain', 'Sujet'),
    ('Rural', 'Sujet'),
    ('Fleurs', 'Motif'),
    ('Animaux', 'Motif'),
    ('Personnages', 'Motif');

-- 9. Association de tags aux œuvres
INSERT INTO ArtworkTag (ArtworkId, TagId)
SELECT 
    a.Id,
    t.Id
FROM 
    Artwork a,
    Tag t
WHERE random() < 0.3
LIMIT 25;

-- 10. Ajout d'images supplémentaires pour certaines œuvres
INSERT INTO ArtworkImage (ArtworkId, ImageUrl, Caption, IsPrimary)
SELECT 
    a.Id,
    'https://example.com/images/detail' || generate_series(1, 20) || '.jpg',
    'Vue détaillée ' || generate_series(1, 20),
    random() < 0.2
FROM 
    Artwork a
WHERE random() < 0.5
LIMIT 20;

-- 11. Ajout d'entrées de métadonnées
INSERT INTO JocondeMetadata (SourceVersion, LastUpdateDate, TotalRecords, SchemaVersion, Notes)
VALUES
    ('Joconde 2024-03', '2024-03-01', 500000, '3.2', 'Dernière version complète de la base Joconde');

-- 12. Ajout d'entrées dans le journal de synchronisation
INSERT INTO DataSyncLog (SyncType, StartedAt, CompletedAt, Status, ItemsProcessed)
VALUES
    ('Full', '2025-03-20 10:00:00', '2025-03-20 12:30:00', 'Completed', 500000),
    ('Incremental', '2025-03-22 08:00:00', '2025-03-22 08:15:00', 'Completed', 1250);
