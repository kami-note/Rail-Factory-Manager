using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Logistics.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalTaxesToShipmentItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CofinsCst",
                table: "shipment_items",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IcmsCst",
                table: "shipment_items",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "IcmsOrigin",
                table: "shipment_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "IpiRate",
                table: "shipment_items",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PisCst",
                table: "shipment_items",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CofinsCst",
                table: "shipment_items");

            migrationBuilder.DropColumn(
                name: "IcmsCst",
                table: "shipment_items");

            migrationBuilder.DropColumn(
                name: "IcmsOrigin",
                table: "shipment_items");

            migrationBuilder.DropColumn(
                name: "IpiRate",
                table: "shipment_items");

            migrationBuilder.DropColumn(
                name: "PisCst",
                table: "shipment_items");
        }
    }
}
