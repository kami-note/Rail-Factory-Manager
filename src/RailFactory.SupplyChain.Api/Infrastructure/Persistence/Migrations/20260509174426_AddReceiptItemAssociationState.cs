using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptItemAssociationState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AssociationConversionFactor",
                table: "material_receipt_items",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssociationReason",
                table: "material_receipt_items",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssociationStatus",
                table: "material_receipt_items",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AssociationUpdatedAt",
                table: "material_receipt_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "AssociationUpdatedBy",
                table: "material_receipt_items",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternalMaterialCode",
                table: "material_receipt_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierProductCode",
                table: "material_receipt_items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE material_receipt_items AS i
                SET
                    "SupplierProductCode" = i."MaterialCode",
                    "InternalMaterialCode" = CASE
                        WHEN r."Status" = 'PendingAssociation' THEN NULL
                        ELSE i."MaterialCode"
                    END,
                    "AssociationStatus" = CASE
                        WHEN r."Status" = 'PendingAssociation' THEN 'Pending'
                        ELSE 'Mapped'
                    END,
                    "AssociationConversionFactor" = CASE
                        WHEN r."Status" = 'PendingAssociation' THEN NULL
                        ELSE 1.0000
                    END,
                    "AssociationUpdatedAt" = COALESCE(r."CreatedAt", CURRENT_TIMESTAMP),
                    "AssociationUpdatedBy" = 'system@railfactory.local'
                FROM material_receipts AS r
                WHERE i."ReceiptId" = r."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssociationConversionFactor",
                table: "material_receipt_items");

            migrationBuilder.DropColumn(
                name: "AssociationReason",
                table: "material_receipt_items");

            migrationBuilder.DropColumn(
                name: "AssociationStatus",
                table: "material_receipt_items");

            migrationBuilder.DropColumn(
                name: "AssociationUpdatedAt",
                table: "material_receipt_items");

            migrationBuilder.DropColumn(
                name: "AssociationUpdatedBy",
                table: "material_receipt_items");

            migrationBuilder.DropColumn(
                name: "InternalMaterialCode",
                table: "material_receipt_items");

            migrationBuilder.DropColumn(
                name: "SupplierProductCode",
                table: "material_receipt_items");
        }
    }
}
