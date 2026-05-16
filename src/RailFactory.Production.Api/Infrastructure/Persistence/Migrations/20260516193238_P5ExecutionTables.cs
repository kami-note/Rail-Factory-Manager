using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Production.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P5ExecutionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "production_outbox",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "production_outbox",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "consumption_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ConsumedQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    InventoryBalanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consumption_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "quality_inspections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Result = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    InspectedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    InspectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quality_inspections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "scrap_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ScrapQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scrap_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_consumption_records_ProductionOrderId",
                table: "consumption_records",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_quality_inspections_ProductionOrderId_InspectedAt",
                table: "quality_inspections",
                columns: new[] { "ProductionOrderId", "InspectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_scrap_records_ProductionOrderId",
                table: "scrap_records",
                column: "ProductionOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consumption_records");

            migrationBuilder.DropTable(
                name: "quality_inspections");

            migrationBuilder.DropTable(
                name: "scrap_records");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "production_outbox");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "production_outbox");
        }
    }
}
