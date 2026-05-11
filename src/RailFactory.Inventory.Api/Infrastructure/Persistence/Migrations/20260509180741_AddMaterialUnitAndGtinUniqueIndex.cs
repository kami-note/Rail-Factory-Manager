using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Inventory.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialUnitAndGtinUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UnitOfMeasure",
                table: "materials",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "UN");

            migrationBuilder.CreateIndex(
                name: "IX_materials_Gtin",
                table: "materials",
                column: "Gtin",
                unique: true,
                filter: "\"Gtin\" IS NOT NULL AND \"Gtin\" <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_materials_Gtin",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "UnitOfMeasure",
                table: "materials");
        }
    }
}
