using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNcmCfopEanToReceiptItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cfop",
                table: "material_receipt_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ean",
                table: "material_receipt_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ncm",
                table: "material_receipt_items",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cfop",
                table: "material_receipt_items");

            migrationBuilder.DropColumn(
                name: "Ean",
                table: "material_receipt_items");

            migrationBuilder.DropColumn(
                name: "Ncm",
                table: "material_receipt_items");
        }
    }
}
