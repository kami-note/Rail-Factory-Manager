using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplyOutboxDeadLetterState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "supply_outbox_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeadLetteredAt",
                table: "supply_outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "supply_outbox_messages",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastAttemptAt",
                table: "supply_outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "supply_outbox_messages",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.Sql(
                """
                UPDATE supply_outbox_messages
                SET "Status" = 'Dispatched'
                WHERE "DispatchedAt" IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_supply_outbox_messages_Status_CreatedAt",
                table: "supply_outbox_messages",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_supply_outbox_messages_Status_CreatedAt",
                table: "supply_outbox_messages");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "supply_outbox_messages");

            migrationBuilder.DropColumn(
                name: "DeadLetteredAt",
                table: "supply_outbox_messages");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "supply_outbox_messages");

            migrationBuilder.DropColumn(
                name: "LastAttemptAt",
                table: "supply_outbox_messages");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "supply_outbox_messages");
        }
    }
}
