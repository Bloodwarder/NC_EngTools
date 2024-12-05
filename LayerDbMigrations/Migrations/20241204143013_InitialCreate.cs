using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LayerDbMigrations.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LayerGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MainName = table.Column<string>(type: "TEXT", nullable: false),
                    LayerLegendData_Rank = table.Column<int>(type: "INTEGER", nullable: false),
                    LayerLegendData_Label = table.Column<string>(type: "TEXT", nullable: false),
                    LayerLegendData_SubLabel = table.Column<string>(type: "TEXT", nullable: true),
                    LayerLegendData_IgnoreLayer = table.Column<bool>(type: "INTEGER", nullable: false),
                    AlternateLayer = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("LayerGroupDataPrimaryKey", x => x.Id);
                    table.UniqueConstraint("LayerGroupDataAlternateKey", x => x.MainName);
                });

            migrationBuilder.CreateTable(
                name: "LayerData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MainName = table.Column<string>(type: "TEXT", nullable: false),
                    StatusName = table.Column<string>(type: "TEXT", nullable: false),
                    LayerGroupMainName = table.Column<string>(type: "TEXT", nullable: true),
                    LayerPropertiesData_ConstantWidth = table.Column<double>(type: "REAL", nullable: true),
                    LayerPropertiesData_LinetypeScale = table.Column<double>(type: "REAL", nullable: true),
                    LayerPropertiesData_Red = table.Column<byte>(type: "INTEGER", nullable: true),
                    LayerPropertiesData_Green = table.Column<byte>(type: "INTEGER", nullable: true),
                    LayerPropertiesData_Blue = table.Column<byte>(type: "INTEGER", nullable: true),
                    LayerPropertiesData_LinetypeName = table.Column<string>(type: "TEXT", nullable: true),
                    LayerPropertiesData_LineWeight = table.Column<int>(type: "INTEGER", nullable: true),
                    LayerDrawTemplateData_DrawTemplate = table.Column<string>(type: "TEXT", nullable: true),
                    LayerDrawTemplateData_MarkChar = table.Column<string>(type: "TEXT", nullable: true),
                    LayerDrawTemplateData_Width = table.Column<string>(type: "TEXT", nullable: true),
                    LayerDrawTemplateData_Height = table.Column<string>(type: "TEXT", nullable: true),
                    LayerDrawTemplateData_InnerBorderBrightness = table.Column<double>(type: "REAL", nullable: true),
                    LayerDrawTemplateData_InnerHatchPattern = table.Column<string>(type: "TEXT", nullable: true),
                    LayerDrawTemplateData_InnerHatchScale = table.Column<double>(type: "REAL", nullable: true),
                    LayerDrawTemplateData_InnerHatchBrightness = table.Column<double>(type: "REAL", nullable: true),
                    LayerDrawTemplateData_InnerHatchAngle = table.Column<double>(type: "REAL", nullable: true),
                    LayerDrawTemplateData_FenceWidth = table.Column<string>(type: "TEXT", nullable: true),
                    LayerDrawTemplateData_FenceHeight = table.Column<string>(type: "TEXT", nullable: true),
                    LayerDrawTemplateData_FenceLayer = table.Column<string>(type: "TEXT", nullable: true),
                    LayerDrawTemplateData_OuterHatchPattern = table.Column<string>(type: "TEXT", nullable: true),
                    LayerDrawTemplateData_OuterHatchScale = table.Column<double>(type: "REAL", nullable: true),
                    LayerDrawTemplateData_OuterHatchBrightness = table.Column<double>(type: "REAL", nullable: true),
                    LayerDrawTemplateData_OuterHatchAngle = table.Column<double>(type: "REAL", nullable: true),
                    LayerDrawTemplateData_Radius = table.Column<double>(type: "REAL", nullable: true),
                    LayerDrawTemplateData_BlockName = table.Column<string>(type: "TEXT", nullable: true),
                    LayerDrawTemplateData_BlockXOffset = table.Column<double>(type: "REAL", nullable: true),
                    LayerDrawTemplateData_BlockYOffset = table.Column<double>(type: "REAL", nullable: true),
                    LayerDrawTemplateData_BlockPath = table.Column<string>(type: "TEXT", nullable: true),
                    Separator = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("LayerDataPrimaryKey", x => x.Id);
                    table.UniqueConstraint("LayerDataAlternateKey", x => new { x.MainName, x.Separator, x.StatusName });
                    table.ForeignKey(
                        name: "FK_LayerData_LayerGroups_LayerGroupMainName",
                        column: x => x.LayerGroupMainName,
                        principalTable: "LayerGroups",
                        principalColumn: "MainName");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LayerData_LayerGroupMainName",
                table: "LayerData",
                column: "LayerGroupMainName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LayerData");

            migrationBuilder.DropTable(
                name: "LayerGroups");
        }
    }
}
