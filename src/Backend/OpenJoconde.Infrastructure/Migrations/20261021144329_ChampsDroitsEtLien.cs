using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenJoconde.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChampsDroitsEtLien : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "artist_under_copyright",
                table: "artwork",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "associated_site_url",
                table: "artwork",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "public_domain_year",
                table: "artwork",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "artist_under_copyright",
                table: "artwork");

            migrationBuilder.DropColumn(
                name: "associated_site_url",
                table: "artwork");

            migrationBuilder.DropColumn(
                name: "public_domain_year",
                table: "artwork");
        }
    }
}
