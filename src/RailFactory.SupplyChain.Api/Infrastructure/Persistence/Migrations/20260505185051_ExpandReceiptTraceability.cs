using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandReceiptTraceability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessKey",
                table: "material_receipts",
                type: "character varying(44)",
                maxLength: 44,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawXml",
                table: "material_receipts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalValue",
                table: "material_receipts",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalDescription",
                table: "material_receipt_items",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "material_receipt_items",
                type: "numeric(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessKey",
                table: "material_receipts");

            migrationBuilder.DropColumn(
                name: "RawXml",
                table: "material_receipts");

            migrationBuilder.DropColumn(
                name: "TotalValue",
                table: "material_receipts");

            migrationBuilder.DropColumn(
                name: "OriginalDescription",
                table: "material_receipt_items");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "material_receipt_items");
        }
    }
}
