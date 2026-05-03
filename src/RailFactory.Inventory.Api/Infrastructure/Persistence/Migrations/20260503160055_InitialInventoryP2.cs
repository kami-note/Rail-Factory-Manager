using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Inventory.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialInventoryP2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_balances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MaterialCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    StockLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceReference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_balances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_ledger_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BalanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Operation = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    QuantityDelta = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    DetailsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_ledger_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_processed_integration_messages",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_processed_integration_messages", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "stock_locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_locations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_TenantCode_SourceReference",
                table: "inventory_balances",
                columns: new[] { "TenantCode", "SourceReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_ledger_entries_TenantCode_BalanceId_CreatedAt",
                table: "inventory_ledger_entries",
                columns: new[] { "TenantCode", "BalanceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_locations_TenantCode_Code",
                table: "stock_locations",
                columns: new[] { "TenantCode", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_balances");

            migrationBuilder.DropTable(
                name: "inventory_ledger_entries");

            migrationBuilder.DropTable(
                name: "inventory_processed_integration_messages");

            migrationBuilder.DropTable(
                name: "stock_locations");
        }
    }
}
