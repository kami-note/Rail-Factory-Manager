using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConferenceFieldsToReceiptItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConfirmedExpirationDate",
                table: "material_receipt_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmedLotNumber",
                table: "material_receipt_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CountedQuantity",
                table: "material_receipt_items",
                type: "numeric(18,4)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmedExpirationDate",
                table: "material_receipt_items");

            migrationBuilder.DropColumn(
                name: "ConfirmedLotNumber",
                table: "material_receipt_items");

            migrationBuilder.DropColumn(
                name: "CountedQuantity",
                table: "material_receipt_items");
        }
    }
}
