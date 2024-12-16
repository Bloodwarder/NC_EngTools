using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LayerDbMigrations.Migrations
{
    /// <inheritdoc />
    public partial class ZoneInfoFilterAndIgnoreWidth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdditionalFilter",
                table: "ZoneInfo",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IgnoreConstructionWidth",
                table: "ZoneInfo",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalFilter",
                table: "ZoneInfo");

            migrationBuilder.DropColumn(
                name: "IgnoreConstructionWidth",
                table: "ZoneInfo");
        }
    }
}
