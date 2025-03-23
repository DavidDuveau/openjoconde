# Mises à jour nécessaires pour le OpenJocondeDbContext

Ce document décrit les modifications à apporter au fichier `OpenJocondeDbContext.cs` pour prendre en compte les nouvelles tables et relations ajoutées dans les scripts de migration.

## Nouvelles classes de modèle à ajouter dans le projet Core

Les classes suivantes doivent être ajoutées au projet OpenJoconde.Core pour représenter les nouvelles entités:

```csharp
// Dans OpenJoconde.Core.Models

public class DataSyncLog
{
    public Guid Id { get; set; }
    public string SyncType { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; }
    public int ItemsProcessed { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UsageStatistics
{
    public Guid Id { get; set; }
    public string EventType { get; set; }
    public DateTime EventDate { get; set; }
    public string UserId { get; set; }
    public string Parameters { get; set; } // Stocké comme JSON
    public DateTime CreatedAt { get; set; }
}

public class ArtworkImage
{
    public Guid Id { get; set; }
    public Guid ArtworkId { get; set; }
    public string ImageUrl { get; set; }
    public string Caption { get; set; }
    public bool IsPrimary { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    
    public virtual Artwork Artwork { get; set; }
}

public class JocondeMetadata
{
    public Guid Id { get; set; }
    public string SourceVersion { get; set; }
    public DateTime? LastUpdateDate { get; set; }
    public int? TotalRecords { get; set; }
    public string SchemaVersion { get; set; }
    public string Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Tag
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    
    public virtual ICollection<Artwork> Artworks { get; set; } = new List<Artwork>();
}

// Ajout dans la classe Artwork
public virtual ICollection<ArtworkImage> Images { get; set; } = new List<ArtworkImage>();
public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
```

## Modifications à apporter au OpenJocondeDbContext

Ajouter les DbSet pour les nouvelles entités:

```csharp
public DbSet<DataSyncLog> DataSyncLogs { get; set; }
public DbSet<UsageStatistics> UsageStatistics { get; set; }
public DbSet<ArtworkImage> ArtworkImages { get; set; }
public DbSet<JocondeMetadata> JocondeMetadata { get; set; }
public DbSet<Tag> Tags { get; set; }
```

Ajouter les configurations d'entités dans la méthode OnModelCreating:

```csharp
// Configuration de DataSyncLog
modelBuilder.Entity<DataSyncLog>(entity =>
{
    entity.ToTable("DataSyncLog");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Id).ValueGeneratedOnAdd();
    entity.Property(e => e.SyncType).HasMaxLength(50).IsRequired();
    entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
});

// Configuration de UsageStatistics
modelBuilder.Entity<UsageStatistics>(entity =>
{
    entity.ToTable("UsageStatistics");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Id).ValueGeneratedOnAdd();
    entity.Property(e => e.EventType).HasMaxLength(50).IsRequired();
    entity.Property(e => e.UserId).HasMaxLength(100);
    entity.Property(e => e.Parameters).HasColumnType("jsonb");
});

// Configuration de ArtworkImage
modelBuilder.Entity<ArtworkImage>(entity =>
{
    entity.ToTable("ArtworkImage");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Id).ValueGeneratedOnAdd();
    entity.Property(e => e.ImageUrl).HasMaxLength(1000).IsRequired();
    entity.Property(e => e.Caption).HasMaxLength(500);
    
    entity.HasOne(e => e.Artwork)
        .WithMany(a => a.Images)
        .HasForeignKey(e => e.ArtworkId)
        .OnDelete(DeleteBehavior.Cascade);
});

// Configuration de JocondeMetadata
modelBuilder.Entity<JocondeMetadata>(entity =>
{
    entity.ToTable("JocondeMetadata");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Id).ValueGeneratedOnAdd();
    entity.Property(e => e.SourceVersion).HasMaxLength(100);
    entity.Property(e => e.SchemaVersion).HasMaxLength(50);
});

// Configuration de Tag
modelBuilder.Entity<Tag>(entity =>
{
    entity.ToTable("Tag");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Id).ValueGeneratedOnAdd();
    entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
    entity.Property(e => e.Category).HasMaxLength(50);
    entity.HasIndex(e => e.Name).IsUnique();
});

// Configuration de la relation Artwork-Tag
modelBuilder.Entity<Artwork>()
    .HasMany(a => a.Tags)
    .WithMany(t => t.Artworks)
    .UsingEntity(j => j.ToTable("ArtworkTag"));
```

## Remarques importantes

1. Ces modifications doivent être appliquées après avoir exécuté les scripts SQL de mise à jour
2. Assurez-vous que les types de données correspondent entre le schéma SQL et la configuration EF Core
3. N'oubliez pas de mettre à jour les référentiels et services correspondants pour utiliser ces nouvelles entités
