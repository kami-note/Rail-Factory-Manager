using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Inventory.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnrichMaterialTechnicalMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Gtin",
                table: "materials",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ncm",
                table: "materials",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gtin",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "Ncm",
                table: "materials");
        }
    }
}
