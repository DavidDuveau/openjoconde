using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenJoconde.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IncrementalSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArtworksRemoved",
                table: "DataSyncLog",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ArtworksUnchanged",
                table: "DataSyncLog",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SourceETag",
                table: "DataSyncLog",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ContentHash",
                table: "Artwork",
                type: "binary(16)",
                fixedLength: true,
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArtworksRemoved",
                table: "DataSyncLog");

            migrationBuilder.DropColumn(
                name: "ArtworksUnchanged",
                table: "DataSyncLog");

            migrationBuilder.DropColumn(
                name: "SourceETag",
                table: "DataSyncLog");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "Artwork");
        }
    }
}
