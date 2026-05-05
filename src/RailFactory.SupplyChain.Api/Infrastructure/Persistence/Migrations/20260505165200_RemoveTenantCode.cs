using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTenantCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_material_receipts_TenantCode_ReceiptNumber",
                table: "material_receipts");

            migrationBuilder.DropColumn(
                name: "TenantCode",
                table: "supply_outbox_messages");

            migrationBuilder.DropColumn(
                name: "TenantCode",
                table: "supply_audit_entries");

            migrationBuilder.DropColumn(
                name: "TenantCode",
                table: "material_receipts");

            migrationBuilder.CreateIndex(
                name: "IX_material_receipts_ReceiptNumber",
                table: "material_receipts",
                column: "ReceiptNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_material_receipts_ReceiptNumber",
                table: "material_receipts");

            migrationBuilder.AddColumn<string>(
                name: "TenantCode",
                table: "supply_outbox_messages",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantCode",
                table: "supply_audit_entries",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantCode",
                table: "material_receipts",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_material_receipts_TenantCode_ReceiptNumber",
                table: "material_receipts",
                columns: new[] { "TenantCode", "ReceiptNumber" },
                unique: true);
        }
    }
}
