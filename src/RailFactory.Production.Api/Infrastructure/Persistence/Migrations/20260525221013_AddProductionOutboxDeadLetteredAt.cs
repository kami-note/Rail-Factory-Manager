using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Production.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionOutboxDeadLetteredAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_production_outbox_DispatchedAt",
                table: "production_outbox");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeadLetteredAt",
                table: "production_outbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_outbox_DispatchedAt_DeadLetteredAt",
                table: "production_outbox",
                columns: new[] { "DispatchedAt", "DeadLetteredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_production_outbox_DispatchedAt_DeadLetteredAt",
                table: "production_outbox");

            migrationBuilder.DropColumn(
                name: "DeadLetteredAt",
                table: "production_outbox");

            migrationBuilder.CreateIndex(
                name: "IX_production_outbox_DispatchedAt",
                table: "production_outbox",
                column: "DispatchedAt");
        }
    }
}
