using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenJoconde.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ContexteMusee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "museum",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "highlights",
                table: "museum",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "thematic_domains",
                table: "museum",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category",
                table: "museum");

            migrationBuilder.DropColumn(
                name: "highlights",
                table: "museum");

            migrationBuilder.DropColumn(
                name: "thematic_domains",
                table: "museum");
        }
    }
}
