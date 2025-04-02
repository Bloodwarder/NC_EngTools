using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LayerDbMigrations.Migrations
{
    /// <inheritdoc />
    public partial class DrawOrderGroupsAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DrawOrderGroupId",
                table: "LayerData",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DrawOrderGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrawOrderGroups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LayerData_DrawOrderGroupId",
                table: "LayerData",
                column: "DrawOrderGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_LayerData_DrawOrderGroups_DrawOrderGroupId",
                table: "LayerData",
                column: "DrawOrderGroupId",
                principalTable: "DrawOrderGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LayerData_DrawOrderGroups_DrawOrderGroupId",
                table: "LayerData");

            migrationBuilder.DropTable(
                name: "DrawOrderGroups");

            migrationBuilder.DropIndex(
                name: "IX_LayerData_DrawOrderGroupId",
                table: "LayerData");

            migrationBuilder.DropColumn(
                name: "DrawOrderGroupId",
                table: "LayerData");
        }
    }
}
