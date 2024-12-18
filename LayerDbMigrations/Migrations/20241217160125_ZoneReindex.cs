using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LayerDbMigrations.Migrations
{
    /// <inheritdoc />
    public partial class ZoneReindex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ZoneInfo_SourceLayerId_ZoneLayerId",
                table: "ZoneInfo");

            migrationBuilder.CreateIndex(
                name: "IX_ZoneInfo_SourceLayerId_ZoneLayerId_AdditionalFilter",
                table: "ZoneInfo",
                columns: new[] { "SourceLayerId", "ZoneLayerId", "AdditionalFilter" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ZoneInfo_SourceLayerId_ZoneLayerId_AdditionalFilter",
                table: "ZoneInfo");

            migrationBuilder.CreateIndex(
                name: "IX_ZoneInfo_SourceLayerId_ZoneLayerId",
                table: "ZoneInfo",
                columns: new[] { "SourceLayerId", "ZoneLayerId" },
                unique: true);
        }
    }
}
