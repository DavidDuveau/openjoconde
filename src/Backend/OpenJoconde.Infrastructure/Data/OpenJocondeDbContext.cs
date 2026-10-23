using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using OpenJoconde.Core.Models;
using System;

namespace OpenJoconde.Infrastructure.Data
{
    /// <summary>
    /// Entity Framework Core DbContext for OpenJoconde
    /// </summary>
    public class OpenJocondeDbContext : DbContext
    {
        public OpenJocondeDbContext(DbContextOptions<OpenJocondeDbContext> options)
            : base(options)
        {
        }

        /// <summary>Colonne tsvector des œuvres, calculée par la base</summary>
        public const string SearchVector = "search_vector";

        /// <summary>Titre sans la ponctuation initiale, calculé par la base : clé du tri alphabétique des œuvres</summary>
        public const string TitleSort = "title_sort";

        /// <summary>Première année de la date de création, calculée par la base : clé du tri par date</summary>
        public const string CreationYear = "creation_year";

        public DbSet<Artwork> Artworks { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Domain> Domains { get; set; }
        public DbSet<Technique> Techniques { get; set; }
        public DbSet<Period> Periods { get; set; }
        public DbSet<Museum> Museums { get; set; }
        public DbSet<DataSyncLog> DataSyncLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // citext : comparaisons et index uniques insensibles à la casse ; unaccent : plein texte sans accents
            modelBuilder.HasPostgresExtension("citext");
            modelBuilder.HasPostgresExtension("unaccent");

            // Configure Artwork entity
            modelBuilder.Entity<Artwork>(entity =>
            {
                entity.ToTable("artwork");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Reference).HasColumnType("citext").IsRequired();
                entity.Property(e => e.InventoryNumber).HasColumnType("citext");
                entity.Property(e => e.Title).HasMaxLength(500);
                entity.Property(e => e.Description).HasColumnType("text");
                entity.HasIndex(e => e.Reference).IsUnique();
                entity.HasIndex(e => e.InventoryNumber);
                entity.HasIndex(e => e.Title);
                entity.Property(e => e.ContentHash);
                entity.HasIndex(e => e.MuseumId);
                // Vecteur de recherche plein texte (titre et description), calculé par la base ; hors de l'entité
                entity.Property<NpgsqlTsVector>(SearchVector)
                    .HasComputedColumnSql("to_tsvector('french_unaccent', coalesce(title, '') || ' ' || coalesce(description, ''))", stored: true);
                entity.HasIndex(SearchVector).HasMethod("GIN");
                // Clé de tri : « - Ah ! docteur… » se range à A, pas avant tous les titres. L'identifiant départage les
                // titres égaux, pour que la pagination ne répète ni ne saute aucune œuvre
                entity.Property<string>(TitleSort)
                    .HasComputedColumnSql("ltrim(title, ' -.,;:!?''\"«»([{*/_')", stored: true);
                entity.HasIndex(TitleSort, "Id");
                // Première année de la date de création, lue dans le texte libre (« 1er quart 20e siècle » : 1900) par la
                // fonction SQL oj_creation_year, pour trier par date
                entity.Property<int?>(CreationYear)
                    .HasComputedColumnSql("oj_creation_year(creation_date)", stored: true);
                entity.HasIndex(CreationYear, "Id");
                entity.HasOne(e => e.Museum)
                    .WithMany()
                    .HasForeignKey(e => e.MuseumId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Artist entity
            modelBuilder.Entity<Artist>(entity =>
            {
                entity.ToTable("artist");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.LastName).HasColumnType("citext").IsRequired();
                entity.Property(e => e.FirstName).HasColumnType("citext");
                entity.Property(e => e.BirthDate).HasMaxLength(100);
                entity.Property(e => e.DeathDate).HasMaxLength(100);
                entity.Property(e => e.Nationality).HasMaxLength(200);
                entity.Property(e => e.Biography).HasColumnType("text");
                entity.Property(e => e.WikidataId).HasMaxLength(20);
                entity.Property(e => e.WikipediaUrl).HasMaxLength(500);
                entity.Property(e => e.Movement).HasMaxLength(200);
                entity.HasIndex(e => e.EnrichmentAttemptedAt);
                entity.HasIndex(e => e.LastName);
            });

            // Configure ArtworkArtist join entity
            modelBuilder.Entity<ArtworkArtist>(entity =>
            {
                entity.ToTable("artwork_artist");
                entity.HasKey(e => new { e.ArtworkId, e.ArtistId });
                
                entity.HasOne(e => e.Artwork)
                    .WithMany(e => e.Artists)
                    .HasForeignKey(e => e.ArtworkId);
                
                entity.HasOne(e => e.Artist)
                    .WithMany(e => e.Artworks)
                    .HasForeignKey(e => e.ArtistId);
                
                entity.Property(e => e.Role).HasMaxLength(100);
            });

            // Configure Domain entity
            modelBuilder.Entity<Domain>(entity =>
            {
                entity.ToTable("domain");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).HasColumnType("citext").IsRequired();
                entity.Property(e => e.Description).HasColumnType("text");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure Technique entity
            modelBuilder.Entity<Technique>(entity =>
            {
                entity.ToTable("technique");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).HasColumnType("citext").IsRequired();
                entity.Property(e => e.Description).HasColumnType("text");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure Period entity
            modelBuilder.Entity<Period>(entity =>
            {
                entity.ToTable("period");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).HasColumnType("citext").IsRequired();
                entity.Property(e => e.Description).HasColumnType("text");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure Museum entity
            modelBuilder.Entity<Museum>(entity =>
            {
                entity.ToTable("museum");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).HasColumnType("citext").IsRequired();
                entity.Property(e => e.City).HasMaxLength(200);
                entity.Property(e => e.Department).HasMaxLength(200);
                entity.Property(e => e.ZipCode).HasMaxLength(20);
                entity.Property(e => e.Phone).HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(200);
                entity.Property(e => e.Website).HasMaxLength(500);
                entity.Property(e => e.Address).HasColumnType("text");
                entity.Property(e => e.Description).HasColumnType("text");
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.City);
            });

            // Configure DataSyncLog entity
            modelBuilder.Entity<DataSyncLog>(entity =>
            {
                entity.ToTable("data_sync_log");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.SyncType).HasMaxLength(50).IsRequired();
                entity.Property(e => e.TriggeredBy).HasMaxLength(20).IsRequired();
                entity.Property(e => e.SourceETag).HasMaxLength(200);
                entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
                entity.HasIndex(e => e.StartedAt);
            });

            // Configure many-to-many relationships
            modelBuilder.Entity<Artwork>()
                .HasMany(a => a.Domains)
                .WithMany(d => d.Artworks)
                .UsingEntity(j => j.ToTable("artwork_domain"));

            modelBuilder.Entity<Artwork>()
                .HasMany(a => a.Techniques)
                .WithMany(t => t.Artworks)
                .UsingEntity(j => j.ToTable("artwork_technique"));

            modelBuilder.Entity<Artwork>()
                .HasMany(a => a.Periods)
                .WithMany(p => p.Artworks)
                .UsingEntity(j => j.ToTable("artwork_period"));
        }
    }
}
