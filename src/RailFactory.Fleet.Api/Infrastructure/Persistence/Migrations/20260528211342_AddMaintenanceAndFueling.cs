using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Fleet.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceAndFueling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fueling_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    LitersSupplied = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    PricePerLiter = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    Odometer = table.Column<int>(type: "integer", nullable: true),
                    Supplier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fueling_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CompletedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_plans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fueling_records_Date",
                table: "fueling_records",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_fueling_records_VehicleId",
                table: "fueling_records",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_plans_Status",
                table: "maintenance_plans",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_plans_VehicleId",
                table: "maintenance_plans",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fueling_records");

            migrationBuilder.DropTable(
                name: "maintenance_plans");
        }
    }
}
