using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Logistics.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentDeliveryCoords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryCity",
                table: "shipment_orders",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryLatitudeDeg",
                table: "shipment_orders",
                type: "numeric(10,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryLongitudeDeg",
                table: "shipment_orders",
                type: "numeric(10,6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryCity",
                table: "shipment_orders");

            migrationBuilder.DropColumn(
                name: "DeliveryLatitudeDeg",
                table: "shipment_orders");

            migrationBuilder.DropColumn(
                name: "DeliveryLongitudeDeg",
                table: "shipment_orders");
        }
    }
}
