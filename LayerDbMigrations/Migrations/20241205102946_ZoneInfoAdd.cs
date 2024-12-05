using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LayerDbMigrations.Migrations
{
    /// <inheritdoc />
    public partial class ZoneInfoAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "LayerDataAlternateKey",
                table: "LayerData");

            migrationBuilder.DropColumn(
                name: "Separator",
                table: "LayerData");

            migrationBuilder.AddUniqueConstraint(
                name: "LayerDataAlternateKey",
                table: "LayerData",
                columns: new[] { "MainName", "StatusName" });

            migrationBuilder.CreateTable(
                name: "ZoneInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceLayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ZoneLayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<double>(type: "REAL", nullable: false),
                    DefaultConstructionWidth = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZoneInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZoneInfo_LayerData_SourceLayerId",
                        column: x => x.SourceLayerId,
                        principalTable: "LayerData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ZoneInfo_LayerData_ZoneLayerId",
                        column: x => x.ZoneLayerId,
                        principalTable: "LayerData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ZoneInfo_SourceLayerId",
                table: "ZoneInfo",
                column: "SourceLayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ZoneInfo_ZoneLayerId",
                table: "ZoneInfo",
                column: "ZoneLayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZoneInfo");

            migrationBuilder.DropUniqueConstraint(
                name: "LayerDataAlternateKey",
                table: "LayerData");

            migrationBuilder.AddColumn<string>(
                name: "Separator",
                table: "LayerData",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddUniqueConstraint(
                name: "LayerDataAlternateKey",
                table: "LayerData",
                columns: new[] { "MainName", "Separator", "StatusName" });
        }
    }
}
