using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenJoconde.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TriTitre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "title_sort",
                table: "artwork",
                type: "text",
                nullable: true,
                computedColumnSql: "ltrim(title, ' -.,;:!?''\"«»([{*/_')",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "ix_artwork_title_sort_id",
                table: "artwork",
                columns: new[] { "title_sort", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_artwork_title_sort_id",
                table: "artwork");

            migrationBuilder.DropColumn(
                name: "title_sort",
                table: "artwork");
        }
    }
}
