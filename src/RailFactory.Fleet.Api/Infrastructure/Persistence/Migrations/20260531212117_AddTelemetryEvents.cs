using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Fleet.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelemetryEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vehicle_telemetry_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LatitudeDeg = table.Column<decimal>(type: "numeric(10,6)", nullable: true),
                    LongitudeDeg = table.Column<decimal>(type: "numeric(10,6)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_telemetry_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_telemetry_events_EventType",
                table: "vehicle_telemetry_events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_telemetry_events_VehicleId_OccurredAt",
                table: "vehicle_telemetry_events",
                columns: new[] { "VehicleId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vehicle_telemetry_events");
        }
    }
}
