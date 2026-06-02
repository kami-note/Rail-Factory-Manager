using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Logistics.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalFieldsToShipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NatureOfOperation",
                table: "shipment_orders",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecipientCity",
                table: "shipment_orders",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientCnpj",
                table: "shipment_orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientDistrict",
                table: "shipment_orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientEmail",
                table: "shipment_orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientName",
                table: "shipment_orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientNumber",
                table: "shipment_orders",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientState",
                table: "shipment_orders",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientStreet",
                table: "shipment_orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientZipCode",
                table: "shipment_orders",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CfopCode",
                table: "shipment_items",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "IcmsRate",
                table: "shipment_items",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "NcmCode",
                table: "shipment_items",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TaxBaseIcms",
                table: "shipment_items",
                type: "numeric(14,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitValue",
                table: "shipment_items",
                type: "numeric(14,4)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NatureOfOperation",
                table: "shipment_orders");

            migrationBuilder.DropColumn(
                name: "RecipientCity",
                table: "shipment_orders");

            migrationBuilder.DropColumn(
                name: "RecipientCnpj",
                table: "shipment_orders");

            migrationBuilder.DropColumn(
                name: "RecipientDistrict",
                table: "shipment_orders");

            migrationBuilder.DropColumn(
                name: "RecipientEmail",
                table: "shipment_orders");

            migrationBuilder.DropColumn(
                name: "RecipientName",
                table: "shipment_orders");

            migrationBuilder.DropColumn(
                name: "RecipientNumber",
                table: "shipment_orders");

            migrationBuilder.DropColumn(
                name: "RecipientState",
                table: "shipment_orders");

            migrationBuilder.DropColumn(
                name: "RecipientStreet",
                table: "shipment_orders");

            migrationBuilder.DropColumn(
                name: "RecipientZipCode",
                table: "shipment_orders");

            migrationBuilder.DropColumn(
                name: "CfopCode",
                table: "shipment_items");

            migrationBuilder.DropColumn(
                name: "IcmsRate",
                table: "shipment_items");

            migrationBuilder.DropColumn(
                name: "NcmCode",
                table: "shipment_items");

            migrationBuilder.DropColumn(
                name: "TaxBaseIcms",
                table: "shipment_items");

            migrationBuilder.DropColumn(
                name: "UnitValue",
                table: "shipment_items");
        }
    }
}
