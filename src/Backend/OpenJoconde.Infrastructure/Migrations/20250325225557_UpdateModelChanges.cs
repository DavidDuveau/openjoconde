using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenJoconde.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Artist",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BirthDate = table.Column<string>(type: "text", nullable: false),
                    DeathDate = table.Column<string>(type: "text", nullable: false),
                    Nationality = table.Column<string>(type: "text", nullable: false),
                    Biography = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artist", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Artwork",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InventoryNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Denomination = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Dimensions = table.Column<string>(type: "text", nullable: false),
                    CreationDate = table.Column<string>(type: "text", nullable: false),
                    CreationPlace = table.Column<string>(type: "text", nullable: false),
                    ConservationPlace = table.Column<string>(type: "text", nullable: false),
                    Copyright = table.Column<string>(type: "text", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artwork", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataSyncLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SyncType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArtworksProcessed = table.Column<int>(type: "integer", nullable: false),
                    ArtistsProcessed = table.Column<int>(type: "integer", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSyncLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Domain",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Domain", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JocondeMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastUpdateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalRecords = table.Column<int>(type: "integer", nullable: true),
                    SchemaVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JocondeMetadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Museum",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Department = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    ZipCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Website = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Museum", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Period",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartYear = table.Column<int>(type: "integer", nullable: true),
                    EndYear = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Period", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Technique",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technique", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArtworkArtist",
                columns: table => new
                {
                    ArtworkId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtistId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtworkArtist", x => new { x.ArtworkId, x.ArtistId });
                    table.ForeignKey(
                        name: "FK_ArtworkArtist_Artist_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "Artist",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArtworkArtist_Artwork_ArtworkId",
                        column: x => x.ArtworkId,
                        principalTable: "Artwork",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArtworkDomain",
                columns: table => new
                {
                    ArtworksId = table.Column<Guid>(type: "uuid", nullable: false),
                    DomainsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtworkDomain", x => new { x.ArtworksId, x.DomainsId });
                    table.ForeignKey(
                        name: "FK_ArtworkDomain_Artwork_ArtworksId",
                        column: x => x.ArtworksId,
                        principalTable: "Artwork",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArtworkDomain_Domain_DomainsId",
                        column: x => x.DomainsId,
                        principalTable: "Domain",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArtworkPeriod",
                columns: table => new
                {
                    ArtworksId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtworkPeriod", x => new { x.ArtworksId, x.PeriodsId });
                    table.ForeignKey(
                        name: "FK_ArtworkPeriod_Artwork_ArtworksId",
                        column: x => x.ArtworksId,
                        principalTable: "Artwork",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArtworkPeriod_Period_PeriodsId",
                        column: x => x.PeriodsId,
                        principalTable: "Period",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArtworkTechnique",
                columns: table => new
                {
                    ArtworksId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechniquesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtworkTechnique", x => new { x.ArtworksId, x.TechniquesId });
                    table.ForeignKey(
                        name: "FK_ArtworkTechnique_Artwork_ArtworksId",
                        column: x => x.ArtworksId,
                        principalTable: "Artwork",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArtworkTechnique_Technique_TechniquesId",
                        column: x => x.TechniquesId,
                        principalTable: "Technique",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Artist_LastName",
                table: "Artist",
                column: "LastName");

            migrationBuilder.CreateIndex(
                name: "IX_Artwork_InventoryNumber",
                table: "Artwork",
                column: "InventoryNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Artwork_Reference",
                table: "Artwork",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Artwork_Title",
                table: "Artwork",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_ArtworkArtist_ArtistId",
                table: "ArtworkArtist",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_ArtworkDomain_DomainsId",
                table: "ArtworkDomain",
                column: "DomainsId");

            migrationBuilder.CreateIndex(
                name: "IX_ArtworkPeriod_PeriodsId",
                table: "ArtworkPeriod",
                column: "PeriodsId");

            migrationBuilder.CreateIndex(
                name: "IX_ArtworkTechnique_TechniquesId",
                table: "ArtworkTechnique",
                column: "TechniquesId");

            migrationBuilder.CreateIndex(
                name: "IX_Domain_Name",
                table: "Domain",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Museum_City",
                table: "Museum",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_Museum_Name",
                table: "Museum",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Period_Name",
                table: "Period",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Technique_Name",
                table: "Technique",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArtworkArtist");

            migrationBuilder.DropTable(
                name: "ArtworkDomain");

            migrationBuilder.DropTable(
                name: "ArtworkPeriod");

            migrationBuilder.DropTable(
                name: "ArtworkTechnique");

            migrationBuilder.DropTable(
                name: "DataSyncLog");

            migrationBuilder.DropTable(
                name: "JocondeMetadata");

            migrationBuilder.DropTable(
                name: "Museum");

            migrationBuilder.DropTable(
                name: "Artist");

            migrationBuilder.DropTable(
                name: "Domain");

            migrationBuilder.DropTable(
                name: "Period");

            migrationBuilder.DropTable(
                name: "Artwork");

            migrationBuilder.DropTable(
                name: "Technique");
        }
    }
}
