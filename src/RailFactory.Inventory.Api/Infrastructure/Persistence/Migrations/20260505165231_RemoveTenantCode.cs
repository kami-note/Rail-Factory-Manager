using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Inventory.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTenantCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stock_locations_TenantCode_Code",
                table: "stock_locations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_ledger_entries_TenantCode_BalanceId_CreatedAt",
                table: "inventory_ledger_entries");

            migrationBuilder.DropIndex(
                name: "IX_inventory_balances_TenantCode_SourceReference",
                table: "inventory_balances");

            migrationBuilder.DropColumn(
                name: "TenantCode",
                table: "stock_locations");

            migrationBuilder.DropColumn(
                name: "TenantCode",
                table: "inventory_processed_integration_messages");

            migrationBuilder.DropColumn(
                name: "TenantCode",
                table: "inventory_ledger_entries");

            migrationBuilder.DropColumn(
                name: "TenantCode",
                table: "inventory_balances");

            migrationBuilder.CreateIndex(
                name: "IX_stock_locations_Code",
                table: "stock_locations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_ledger_entries_BalanceId_CreatedAt",
                table: "inventory_ledger_entries",
                columns: new[] { "BalanceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_SourceReference",
                table: "inventory_balances",
                column: "SourceReference",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stock_locations_Code",
                table: "stock_locations");

            migrationBuilder.DropIndex(
                name: "IX_inventory_ledger_entries_BalanceId_CreatedAt",
                table: "inventory_ledger_entries");

            migrationBuilder.DropIndex(
                name: "IX_inventory_balances_SourceReference",
                table: "inventory_balances");

            migrationBuilder.AddColumn<string>(
                name: "TenantCode",
                table: "stock_locations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantCode",
                table: "inventory_processed_integration_messages",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantCode",
                table: "inventory_ledger_entries",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantCode",
                table: "inventory_balances",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_stock_locations_TenantCode_Code",
                table: "stock_locations",
                columns: new[] { "TenantCode", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_ledger_entries_TenantCode_BalanceId_CreatedAt",
                table: "inventory_ledger_entries",
                columns: new[] { "TenantCode", "BalanceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_TenantCode_SourceReference",
                table: "inventory_balances",
                columns: new[] { "TenantCode", "SourceReference" },
                unique: true);
        }
    }
}
