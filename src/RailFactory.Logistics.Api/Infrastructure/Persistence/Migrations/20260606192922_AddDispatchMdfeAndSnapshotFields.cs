using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Logistics.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchMdfeAndSnapshotFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DriverCpf",
                table: "dispatches",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "dispatches",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MdfeAccessKey",
                table: "dispatches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MdfeErrorMessage",
                table: "dispatches",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MdfeExternalId",
                table: "dispatches",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MdfeStatus",
                table: "dispatches",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehiclePlate",
                table: "dispatches",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleRntrc",
                table: "dispatches",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriverCpf",
                table: "dispatches");

            migrationBuilder.DropColumn(
                name: "DriverName",
                table: "dispatches");

            migrationBuilder.DropColumn(
                name: "MdfeAccessKey",
                table: "dispatches");

            migrationBuilder.DropColumn(
                name: "MdfeErrorMessage",
                table: "dispatches");

            migrationBuilder.DropColumn(
                name: "MdfeExternalId",
                table: "dispatches");

            migrationBuilder.DropColumn(
                name: "MdfeStatus",
                table: "dispatches");

            migrationBuilder.DropColumn(
                name: "VehiclePlate",
                table: "dispatches");

            migrationBuilder.DropColumn(
                name: "VehicleRntrc",
                table: "dispatches");
        }
    }
}
