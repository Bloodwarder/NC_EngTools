using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LayerDbMigrations.Migrations
{
    /// <inheritdoc />
    public partial class OldLayersAndGroupsReindex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "AK_LayerGroups_Prefix_MainName",
                table: "LayerGroups");

            migrationBuilder.CreateTable(
                name: "OldLayers",
                columns: table => new
                {
                    OldLayerGroupName = table.Column<string>(type: "TEXT", nullable: false),
                    NewLayerGroupId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OldLayers", x => x.OldLayerGroupName);
                    table.ForeignKey(
                        name: "FK_OldLayers_LayerGroups_NewLayerGroupId",
                        column: x => x.NewLayerGroupId,
                        principalTable: "LayerGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LayerGroups_Prefix_MainName",
                table: "LayerGroups",
                columns: new[] { "Prefix", "MainName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OldLayers_NewLayerGroupId",
                table: "OldLayers",
                column: "NewLayerGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OldLayers");

            migrationBuilder.DropIndex(
                name: "IX_LayerGroups_Prefix_MainName",
                table: "LayerGroups");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_LayerGroups_Prefix_MainName",
                table: "LayerGroups",
                columns: new[] { "Prefix", "MainName" });
        }
    }
}
