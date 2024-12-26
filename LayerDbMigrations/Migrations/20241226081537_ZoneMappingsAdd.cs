using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LayerDbMigrations.Migrations
{
    /// <inheritdoc />
    public partial class ZoneMappingsAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ZoneMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourcePrefix = table.Column<string>(type: "TEXT", nullable: false),
                    SourceStatus = table.Column<string>(type: "TEXT", nullable: false),
                    TargetPrefix = table.Column<string>(type: "TEXT", nullable: false),
                    TargetStatus = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZoneMappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LayerGroups_Prefix",
                table: "LayerGroups",
                column: "Prefix");

            migrationBuilder.CreateIndex(
                name: "IX_ZoneMappings_SourcePrefix_SourceStatus_TargetPrefix_TargetStatus",
                table: "ZoneMappings",
                columns: new[] { "SourcePrefix", "SourceStatus", "TargetPrefix", "TargetStatus" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZoneMappings");

            migrationBuilder.DropIndex(
                name: "IX_LayerGroups_Prefix",
                table: "LayerGroups");
        }
    }
}
