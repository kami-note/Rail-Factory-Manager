using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Inventory.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReservedForOrderId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReservedForOrderId",
                table: "inventory_balances",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_ReservedForOrderId",
                table: "inventory_balances",
                column: "ReservedForOrderId",
                filter: "\"ReservedForOrderId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_inventory_balances_ReservedForOrderId",
                table: "inventory_balances");

            migrationBuilder.DropColumn(
                name: "ReservedForOrderId",
                table: "inventory_balances");
        }
    }
}
