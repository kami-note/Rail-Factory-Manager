using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Production.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_production_outbox_DispatchedAt_DeadLetteredAt",
                table: "production_outbox");

            migrationBuilder.CreateIndex(
                name: "ix_production_outbox_dispatch_poll",
                table: "production_outbox",
                columns: new[] { "DispatchedAt", "DeadLetteredAt", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "ix_production_orders_status",
                table: "production_orders",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_production_outbox_dispatch_poll",
                table: "production_outbox");

            migrationBuilder.DropIndex(
                name: "ix_production_orders_status",
                table: "production_orders");

            migrationBuilder.CreateIndex(
                name: "IX_production_outbox_DispatchedAt_DeadLetteredAt",
                table: "production_outbox",
                columns: new[] { "DispatchedAt", "DeadLetteredAt" });
        }
    }
}
