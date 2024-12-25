using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LayerDbMigrations.Migrations
{
    /// <inheritdoc />
    public partial class KeysAndNamesRework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LayerData_LayerGroups_LayerGroupMainName",
                table: "LayerData");

            migrationBuilder.DropUniqueConstraint(
                name: "LayerGroupDataAlternateKey",
                table: "LayerGroups");

            migrationBuilder.DropPrimaryKey(
                name: "LayerGroupDataPrimaryKey",
                table: "LayerGroups");

            migrationBuilder.DropPrimaryKey(
                name: "LayerDataPrimaryKey",
                table: "LayerData");

            migrationBuilder.DropIndex(
                name: "IX_LayerData_LayerGroupMainName",
                table: "LayerData");

            migrationBuilder.DropIndex(
                name: "IX_LayerData_Prefix_MainName_StatusName",
                table: "LayerData");

            migrationBuilder.DropColumn(
                name: "LayerGroupMainName",
                table: "LayerData");

            migrationBuilder.DropColumn(
                name: "MainName",
                table: "LayerData");

            migrationBuilder.DropColumn(
                name: "Prefix",
                table: "LayerData");

            migrationBuilder.AlterColumn<string>(
                name: "StatusName",
                table: "LayerData",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<int>(
                name: "LayerGroupId",
                table: "LayerData",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LayerPropertiesData_DrawOrderIndex",
                table: "LayerData",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_LayerGroups_Prefix_MainName",
                table: "LayerGroups",
                columns: new[] { "Prefix", "MainName" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_LayerGroups",
                table: "LayerGroups",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LayerData",
                table: "LayerData",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_LayerData_LayerGroupId",
                table: "LayerData",
                column: "LayerGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_LayerData_LayerGroups_LayerGroupId",
                table: "LayerData",
                column: "LayerGroupId",
                principalTable: "LayerGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LayerData_LayerGroups_LayerGroupId",
                table: "LayerData");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_LayerGroups_Prefix_MainName",
                table: "LayerGroups");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LayerGroups",
                table: "LayerGroups");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LayerData",
                table: "LayerData");

            migrationBuilder.DropIndex(
                name: "IX_LayerData_LayerGroupId",
                table: "LayerData");

            migrationBuilder.DropColumn(
                name: "LayerGroupId",
                table: "LayerData");

            migrationBuilder.DropColumn(
                name: "LayerPropertiesData_DrawOrderIndex",
                table: "LayerData");

            migrationBuilder.AlterColumn<string>(
                name: "StatusName",
                table: "LayerData",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LayerGroupMainName",
                table: "LayerData",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MainName",
                table: "LayerData",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Prefix",
                table: "LayerData",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddUniqueConstraint(
                name: "LayerGroupDataAlternateKey",
                table: "LayerGroups",
                column: "MainName");

            migrationBuilder.AddPrimaryKey(
                name: "LayerGroupDataPrimaryKey",
                table: "LayerGroups",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "LayerDataPrimaryKey",
                table: "LayerData",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_LayerData_LayerGroupMainName",
                table: "LayerData",
                column: "LayerGroupMainName");

            migrationBuilder.CreateIndex(
                name: "IX_LayerData_Prefix_MainName_StatusName",
                table: "LayerData",
                columns: new[] { "Prefix", "MainName", "StatusName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LayerData_LayerGroups_LayerGroupMainName",
                table: "LayerData",
                column: "LayerGroupMainName",
                principalTable: "LayerGroups",
                principalColumn: "MainName");
        }
    }
}
