using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Fleet.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialFleetP7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Plate = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Chassis = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    Renavam = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    Type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    MaxWeightKg = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    MaxVolumeCbm = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    LicenseExpiry = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "driver_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_driver_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_driver_assignments_vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_driver_assignments_DriverPersonId",
                table: "driver_assignments",
                column: "DriverPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_driver_assignments_VehicleId_StartDate",
                table: "driver_assignments",
                columns: new[] { "VehicleId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_Plate",
                table: "vehicles",
                column: "Plate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_Status",
                table: "vehicles",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "driver_assignments");

            migrationBuilder.DropTable(
                name: "vehicles");
        }
    }
}
