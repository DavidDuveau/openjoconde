using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenJoconde.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefineDataSyncLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "DataSyncLog");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "DataSyncLog");

            migrationBuilder.AddColumn<int>(
                name: "ErrorCount",
                table: "DataSyncLog",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TriggeredBy",
                table: "DataSyncLog",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.CreateIndex(
                name: "IX_DataSyncLog_StartedAt",
                table: "DataSyncLog",
                column: "StartedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DataSyncLog_StartedAt",
                table: "DataSyncLog");

            migrationBuilder.DropColumn(
                name: "ErrorCount",
                table: "DataSyncLog");

            migrationBuilder.DropColumn(
                name: "TriggeredBy",
                table: "DataSyncLog");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "DataSyncLog",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartTime",
                table: "DataSyncLog",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
