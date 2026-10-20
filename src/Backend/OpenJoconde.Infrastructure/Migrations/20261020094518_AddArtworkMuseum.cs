using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenJoconde.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArtworkMuseum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MuseumId",
                table: "Artwork",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Artwork_MuseumId",
                table: "Artwork",
                column: "MuseumId");

            migrationBuilder.AddForeignKey(
                name: "FK_Artwork_Museum_MuseumId",
                table: "Artwork",
                column: "MuseumId",
                principalTable: "Museum",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Artwork_Museum_MuseumId",
                table: "Artwork");

            migrationBuilder.DropIndex(
                name: "IX_Artwork_MuseumId",
                table: "Artwork");

            migrationBuilder.DropColumn(
                name: "MuseumId",
                table: "Artwork");
        }
    }
}
