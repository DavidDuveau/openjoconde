using Microsoft.EntityFrameworkCore;
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

        public DbSet<Artwork> Artworks { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Domain> Domains { get; set; }
        public DbSet<Technique> Techniques { get; set; }
        public DbSet<Period> Periods { get; set; }
        public DbSet<Museum> Museums { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Artwork entity
            modelBuilder.Entity<Artwork>(entity =>
            {
                entity.ToTable("Artwork");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Reference).HasMaxLength(100).IsRequired();
                entity.Property(e => e.InventoryNumber).HasMaxLength(100);
                entity.Property(e => e.Title).HasMaxLength(250);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.HasIndex(e => e.Reference).IsUnique();
                entity.HasIndex(e => e.InventoryNumber);
                entity.HasIndex(e => e.Title);
            });

            // Configure Artist entity
            modelBuilder.Entity<Artist>(entity =>
            {
                entity.ToTable("Artist");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.HasIndex(e => e.LastName);
            });

            // Configure ArtworkArtist join entity
            modelBuilder.Entity<ArtworkArtist>(entity =>
            {
                entity.ToTable("ArtworkArtist");
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
                entity.ToTable("Domain");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure Technique entity
            modelBuilder.Entity<Technique>(entity =>
            {
                entity.ToTable("Technique");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure Period entity
            modelBuilder.Entity<Period>(entity =>
            {
                entity.ToTable("Period");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure Museum entity
            modelBuilder.Entity<Museum>(entity =>
            {
                entity.ToTable("Museum");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.ZipCode).HasMaxLength(20);
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.City);
            });

            // Configure many-to-many relationships
            modelBuilder.Entity<Artwork>()
                .HasMany(a => a.Domains)
                .WithMany(d => d.Artworks)
                .UsingEntity(j => j.ToTable("ArtworkDomain"));

            modelBuilder.Entity<Artwork>()
                .HasMany(a => a.Techniques)
                .WithMany(t => t.Artworks)
                .UsingEntity(j => j.ToTable("ArtworkTechnique"));

            modelBuilder.Entity<Artwork>()
                .HasMany(a => a.Periods)
                .WithMany(p => p.Artworks)
                .UsingEntity(j => j.ToTable("ArtworkPeriod"));
        }
    }
}
