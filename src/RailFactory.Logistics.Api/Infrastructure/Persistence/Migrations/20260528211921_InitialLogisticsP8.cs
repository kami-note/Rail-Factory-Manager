using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Logistics.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialLogisticsP8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "carriers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RatePerKg = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    RatePerCbm = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carriers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dispatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CarrierId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: true),
                    DriverPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    TrackingCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FreightValueBrl = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ConferencedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DispatchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dispatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "shipment_orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ProductionOrderRef = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipment_orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "shipment_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    VolumeCbm = table.Column<decimal>(type: "numeric(10,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipment_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shipment_items_shipment_orders_ShipmentOrderId",
                        column: x => x.ShipmentOrderId,
                        principalTable: "shipment_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_carriers_DocumentNumber",
                table: "carriers",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_carriers_Status",
                table: "carriers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_dispatches_ShipmentOrderId",
                table: "dispatches",
                column: "ShipmentOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_dispatches_Status",
                table: "dispatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_dispatches_TrackingCode",
                table: "dispatches",
                column: "TrackingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipment_items_ShipmentOrderId",
                table: "shipment_items",
                column: "ShipmentOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_orders_OrderNumber",
                table: "shipment_orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipment_orders_Status",
                table: "shipment_orders",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "carriers");

            migrationBuilder.DropTable(
                name: "dispatches");

            migrationBuilder.DropTable(
                name: "shipment_items");

            migrationBuilder.DropTable(
                name: "shipment_orders");
        }
    }
}
