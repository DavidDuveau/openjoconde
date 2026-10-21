using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenJoconde.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RecherchePrefixeReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Recherche par début de référence ou de numéro d'inventaire : sans ces index, la condition OR avec le
            // plein texte force un balayage de toute la table (environ 1 s pour un million de notices)
            migrationBuilder.Sql("CREATE INDEX ix_artwork_reference_prefixe ON artwork (lower(reference::text) text_pattern_ops)");
            migrationBuilder.Sql("CREATE INDEX ix_artwork_inventory_number_prefixe ON artwork (lower(inventory_number::text) text_pattern_ops)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_artwork_inventory_number_prefixe");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_artwork_reference_prefixe");
        }
    }
}
