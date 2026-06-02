using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Logistics.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchFiscalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FiscalAccessKey",
                table: "dispatches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FiscalExternalId",
                table: "dispatches",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FiscalStatus",
                table: "dispatches",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_dispatches_FiscalExternalId",
                table: "dispatches",
                column: "FiscalExternalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_dispatches_FiscalExternalId",
                table: "dispatches");

            migrationBuilder.DropColumn(
                name: "FiscalAccessKey",
                table: "dispatches");

            migrationBuilder.DropColumn(
                name: "FiscalExternalId",
                table: "dispatches");

            migrationBuilder.DropColumn(
                name: "FiscalStatus",
                table: "dispatches");
        }
    }
}
