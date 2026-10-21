using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace OpenJoconde.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .Annotation("Npgsql:PostgresExtension:unaccent", ",,");

            // Configuration de recherche française sans accents, utilisée par la colonne search_vector de artwork
            migrationBuilder.Sql("CREATE TEXT SEARCH CONFIGURATION french_unaccent (COPY = french)");
            migrationBuilder.Sql("ALTER TEXT SEARCH CONFIGURATION french_unaccent ALTER MAPPING FOR hword, hword_part, word WITH unaccent, french_stem");

            migrationBuilder.CreateTable(
                name: "artist",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_name = table.Column<string>(type: "citext", nullable: false),
                    first_name = table.Column<string>(type: "citext", nullable: true),
                    birth_date = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    death_date = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    nationality = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    biography = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artist", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "data_sync_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sync_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    triggered_by = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    artworks_processed = table.Column<int>(type: "integer", nullable: false),
                    artists_processed = table.Column<int>(type: "integer", nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    error_count = table.Column<int>(type: "integer", nullable: false),
                    artworks_unchanged = table.Column<int>(type: "integer", nullable: false),
                    artworks_removed = table.Column<int>(type: "integer", nullable: false),
                    source_e_tag = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_sync_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "domain",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "citext", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_domain", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "joconde_metadata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_update_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_records = table.Column<int>(type: "integer", nullable: true),
                    schema_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_joconde_metadata", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "museum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "citext", nullable: false),
                    city = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    department = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    zip_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    region = table.Column<string>(type: "text", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_museum", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "period",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "citext", nullable: false),
                    start_year = table.Column<int>(type: "integer", nullable: true),
                    end_year = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_period", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "technique",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "citext", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_technique", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "artwork",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference = table.Column<string>(type: "citext", nullable: false),
                    inventory_number = table.Column<string>(type: "citext", nullable: true),
                    denomination = table.Column<string>(type: "text", nullable: true),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    dimensions = table.Column<string>(type: "text", nullable: true),
                    creation_date = table.Column<string>(type: "text", nullable: true),
                    creation_place = table.Column<string>(type: "text", nullable: true),
                    conservation_place = table.Column<string>(type: "text", nullable: true),
                    copyright = table.Column<string>(type: "text", nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    museum_id = table.Column<Guid>(type: "uuid", nullable: true),
                    content_hash = table.Column<byte[]>(type: "bytea", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "to_tsvector('french_unaccent', coalesce(title, '') || ' ' || coalesce(description, ''))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artwork", x => x.id);
                    table.ForeignKey(
                        name: "fk_artwork_museums_museum_id",
                        column: x => x.museum_id,
                        principalTable: "museum",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "artwork_artist",
                columns: table => new
                {
                    artwork_id = table.Column<Guid>(type: "uuid", nullable: false),
                    artist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artwork_artist", x => new { x.artwork_id, x.artist_id });
                    table.ForeignKey(
                        name: "fk_artwork_artist_artists_artist_id",
                        column: x => x.artist_id,
                        principalTable: "artist",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_artwork_artist_artworks_artwork_id",
                        column: x => x.artwork_id,
                        principalTable: "artwork",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "artwork_domain",
                columns: table => new
                {
                    artworks_id = table.Column<Guid>(type: "uuid", nullable: false),
                    domains_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artwork_domain", x => new { x.artworks_id, x.domains_id });
                    table.ForeignKey(
                        name: "fk_artwork_domain_artworks_artworks_id",
                        column: x => x.artworks_id,
                        principalTable: "artwork",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_artwork_domain_domains_domains_id",
                        column: x => x.domains_id,
                        principalTable: "domain",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "artwork_period",
                columns: table => new
                {
                    artworks_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periods_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artwork_period", x => new { x.artworks_id, x.periods_id });
                    table.ForeignKey(
                        name: "fk_artwork_period_artworks_artworks_id",
                        column: x => x.artworks_id,
                        principalTable: "artwork",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_artwork_period_periods_periods_id",
                        column: x => x.periods_id,
                        principalTable: "period",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "artwork_technique",
                columns: table => new
                {
                    artworks_id = table.Column<Guid>(type: "uuid", nullable: false),
                    techniques_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artwork_technique", x => new { x.artworks_id, x.techniques_id });
                    table.ForeignKey(
                        name: "fk_artwork_technique_artworks_artworks_id",
                        column: x => x.artworks_id,
                        principalTable: "artwork",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_artwork_technique_techniques_techniques_id",
                        column: x => x.techniques_id,
                        principalTable: "technique",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Déclencheurs UpdatedAt : la date de mise à jour suit toute écriture, y compris hors d'EF
            migrationBuilder.Sql(@"CREATE FUNCTION set_updated_at() RETURNS trigger AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END
$$ LANGUAGE plpgsql");
            migrationBuilder.Sql("CREATE TRIGGER update_artwork_timestamp BEFORE UPDATE ON artwork FOR EACH ROW EXECUTE FUNCTION set_updated_at()");
            migrationBuilder.Sql("CREATE TRIGGER update_artist_timestamp BEFORE UPDATE ON artist FOR EACH ROW EXECUTE FUNCTION set_updated_at()");
            migrationBuilder.Sql("CREATE TRIGGER update_domain_timestamp BEFORE UPDATE ON domain FOR EACH ROW EXECUTE FUNCTION set_updated_at()");
            migrationBuilder.Sql("CREATE TRIGGER update_technique_timestamp BEFORE UPDATE ON technique FOR EACH ROW EXECUTE FUNCTION set_updated_at()");
            migrationBuilder.Sql("CREATE TRIGGER update_period_timestamp BEFORE UPDATE ON period FOR EACH ROW EXECUTE FUNCTION set_updated_at()");
            migrationBuilder.Sql("CREATE TRIGGER update_museum_timestamp BEFORE UPDATE ON museum FOR EACH ROW EXECUTE FUNCTION set_updated_at()");

            migrationBuilder.CreateIndex(
                name: "ix_artist_last_name",
                table: "artist",
                column: "last_name");

            migrationBuilder.CreateIndex(
                name: "ix_artwork_inventory_number",
                table: "artwork",
                column: "inventory_number");

            migrationBuilder.CreateIndex(
                name: "ix_artwork_museum_id",
                table: "artwork",
                column: "museum_id");

            migrationBuilder.CreateIndex(
                name: "ix_artwork_reference",
                table: "artwork",
                column: "reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_artwork_search_vector",
                table: "artwork",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "ix_artwork_title",
                table: "artwork",
                column: "title");

            migrationBuilder.CreateIndex(
                name: "ix_artwork_artist_artist_id",
                table: "artwork_artist",
                column: "artist_id");

            migrationBuilder.CreateIndex(
                name: "ix_artwork_domain_domains_id",
                table: "artwork_domain",
                column: "domains_id");

            migrationBuilder.CreateIndex(
                name: "ix_artwork_period_periods_id",
                table: "artwork_period",
                column: "periods_id");

            migrationBuilder.CreateIndex(
                name: "ix_artwork_technique_techniques_id",
                table: "artwork_technique",
                column: "techniques_id");

            migrationBuilder.CreateIndex(
                name: "ix_data_sync_log_started_at",
                table: "data_sync_log",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "ix_domain_name",
                table: "domain",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_museum_city",
                table: "museum",
                column: "city");

            migrationBuilder.CreateIndex(
                name: "ix_museum_name",
                table: "museum",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_period_name",
                table: "period",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_technique_name",
                table: "technique",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "artwork_artist");

            migrationBuilder.DropTable(
                name: "artwork_domain");

            migrationBuilder.DropTable(
                name: "artwork_period");

            migrationBuilder.DropTable(
                name: "artwork_technique");

            migrationBuilder.DropTable(
                name: "data_sync_log");

            migrationBuilder.DropTable(
                name: "joconde_metadata");

            migrationBuilder.DropTable(
                name: "artist");

            migrationBuilder.DropTable(
                name: "domain");

            migrationBuilder.DropTable(
                name: "period");

            migrationBuilder.DropTable(
                name: "artwork");

            migrationBuilder.DropTable(
                name: "technique");

            migrationBuilder.DropTable(
                name: "museum");

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS set_updated_at()");
            migrationBuilder.Sql("DROP TEXT SEARCH CONFIGURATION IF EXISTS french_unaccent");
        }
    }
}
