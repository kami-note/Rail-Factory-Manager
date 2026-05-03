using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSupplyChainP2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "material_receipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReceiptDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TenantCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_receipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FiscalId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "supply_audit_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserIdentifier = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supply_audit_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "supply_outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DispatchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supply_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "material_receipt_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpectedQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_receipt_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_material_receipt_items_material_receipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "material_receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_material_receipt_items_ReceiptId_MaterialCode",
                table: "material_receipt_items",
                columns: new[] { "ReceiptId", "MaterialCode" });

            migrationBuilder.CreateIndex(
                name: "IX_material_receipts_TenantCode_ReceiptNumber",
                table: "material_receipts",
                columns: new[] { "TenantCode", "ReceiptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_FiscalId",
                table: "suppliers",
                column: "FiscalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supply_outbox_messages_DispatchedAt_CreatedAt",
                table: "supply_outbox_messages",
                columns: new[] { "DispatchedAt", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "material_receipt_items");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropTable(
                name: "supply_audit_entries");

            migrationBuilder.DropTable(
                name: "supply_outbox_messages");

            migrationBuilder.DropTable(
                name: "material_receipts");
        }
    }
}
