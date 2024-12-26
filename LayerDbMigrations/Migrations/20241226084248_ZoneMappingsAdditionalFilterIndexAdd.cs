using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LayerDbMigrations.Migrations
{
    /// <inheritdoc />
    public partial class ZoneMappingsAdditionalFilterIndexAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ZoneMappings_SourcePrefix_SourceStatus_TargetPrefix_TargetStatus",
                table: "ZoneMappings");

            migrationBuilder.CreateIndex(
                name: "IX_ZoneMappings_SourcePrefix_SourceStatus_AdditionalFilter_TargetPrefix_TargetStatus",
                table: "ZoneMappings",
                columns: new[] { "SourcePrefix", "SourceStatus", "AdditionalFilter", "TargetPrefix", "TargetStatus" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ZoneMappings_SourcePrefix_SourceStatus_AdditionalFilter_TargetPrefix_TargetStatus",
                table: "ZoneMappings");

            migrationBuilder.CreateIndex(
                name: "IX_ZoneMappings_SourcePrefix_SourceStatus_TargetPrefix_TargetStatus",
                table: "ZoneMappings",
                columns: new[] { "SourcePrefix", "SourceStatus", "TargetPrefix", "TargetStatus" },
                unique: true);
        }
    }
}
