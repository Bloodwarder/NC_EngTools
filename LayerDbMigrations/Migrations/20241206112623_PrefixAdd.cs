using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LayerDbMigrations.Migrations
{
    /// <inheritdoc />
    public partial class PrefixAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ZoneInfo",
                table: "ZoneInfo");

            migrationBuilder.DropIndex(
                name: "IX_ZoneInfo_SourceLayerId",
                table: "ZoneInfo");

            migrationBuilder.DropUniqueConstraint(
                name: "LayerDataAlternateKey",
                table: "LayerData");

            migrationBuilder.AddColumn<string>(
                name: "Prefix",
                table: "LayerGroups",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<byte>(
                name: "LayerPropertiesData_Red",
                table: "LayerData",
                type: "INTEGER",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(byte),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "LayerPropertiesData_LinetypeScale",
                table: "LayerData",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LayerPropertiesData_LinetypeName",
                table: "LayerData",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LayerPropertiesData_LineWeight",
                table: "LayerData",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "LayerPropertiesData_Green",
                table: "LayerData",
                type: "INTEGER",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(byte),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "LayerPropertiesData_ConstantWidth",
                table: "LayerData",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "LayerPropertiesData_Blue",
                table: "LayerData",
                type: "INTEGER",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(byte),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Prefix",
                table: "LayerData",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "ZoneInfo_PrimaryKey",
                table: "ZoneInfo",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ZoneInfo_SourceLayerId_ZoneLayerId",
                table: "ZoneInfo",
                columns: new[] { "SourceLayerId", "ZoneLayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LayerData_Prefix_MainName_StatusName",
                table: "LayerData",
                columns: new[] { "Prefix", "MainName", "StatusName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "ZoneInfo_PrimaryKey",
                table: "ZoneInfo");

            migrationBuilder.DropIndex(
                name: "IX_ZoneInfo_SourceLayerId_ZoneLayerId",
                table: "ZoneInfo");

            migrationBuilder.DropIndex(
                name: "IX_LayerData_Prefix_MainName_StatusName",
                table: "LayerData");

            migrationBuilder.DropColumn(
                name: "Prefix",
                table: "LayerGroups");

            migrationBuilder.DropColumn(
                name: "Prefix",
                table: "LayerData");

            migrationBuilder.AlterColumn<byte>(
                name: "LayerPropertiesData_Red",
                table: "LayerData",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<double>(
                name: "LayerPropertiesData_LinetypeScale",
                table: "LayerData",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<string>(
                name: "LayerPropertiesData_LinetypeName",
                table: "LayerData",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "LayerPropertiesData_LineWeight",
                table: "LayerData",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<byte>(
                name: "LayerPropertiesData_Green",
                table: "LayerData",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<double>(
                name: "LayerPropertiesData_ConstantWidth",
                table: "LayerData",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<byte>(
                name: "LayerPropertiesData_Blue",
                table: "LayerData",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "INTEGER");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ZoneInfo",
                table: "ZoneInfo",
                column: "Id");

            migrationBuilder.AddUniqueConstraint(
                name: "LayerDataAlternateKey",
                table: "LayerData",
                columns: new[] { "MainName", "StatusName" });

            migrationBuilder.CreateIndex(
                name: "IX_ZoneInfo_SourceLayerId",
                table: "ZoneInfo",
                column: "SourceLayerId");
        }
    }
}
