using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenJoconde.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnrichissementArtistes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "enrichment_attempted_at",
                table: "artist",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_artist_enrichment_attempted_at",
                table: "artist",
                column: "enrichment_attempted_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_artist_enrichment_attempted_at",
                table: "artist");

            migrationBuilder.DropColumn(
                name: "enrichment_attempted_at",
                table: "artist");
        }
    }
}
