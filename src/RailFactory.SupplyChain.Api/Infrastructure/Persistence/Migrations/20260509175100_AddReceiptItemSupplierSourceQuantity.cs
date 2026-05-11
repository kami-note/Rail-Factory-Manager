using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptItemSupplierSourceQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SupplierQuantity",
                table: "material_receipt_items",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SupplierUnitOfMeasure",
                table: "material_receipt_items",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE material_receipt_items
                SET
                    "SupplierQuantity" = "ExpectedQuantity",
                    "SupplierUnitOfMeasure" = "UnitOfMeasure";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupplierQuantity",
                table: "material_receipt_items");

            migrationBuilder.DropColumn(
                name: "SupplierUnitOfMeasure",
                table: "material_receipt_items");
        }
    }
}
