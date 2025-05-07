using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenJoconde.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMuseumGeolocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Museum",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Museum",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Museum",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Museum",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Code",
                table: "Museum");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Museum");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Museum");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Museum");
        }
    }
}
