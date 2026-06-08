using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Logistics.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchPaymentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentBoletoUrl",
                table: "dispatches",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentErrorMessage",
                table: "dispatches",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentExternalId",
                table: "dispatches",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentPixUrl",
                table: "dispatches",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "dispatches",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_dispatches_PaymentExternalId",
                table: "dispatches",
                column: "PaymentExternalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_dispatches_PaymentExternalId",
                table: "dispatches");

            migrationBuilder.DropColumn(
                name: "PaymentBoletoUrl",
                table: "dispatches");

            migrationBuilder.DropColumn(
                name: "PaymentErrorMessage",
                table: "dispatches");

            migrationBuilder.DropColumn(
                name: "PaymentExternalId",
                table: "dispatches");

            migrationBuilder.DropColumn(
                name: "PaymentPixUrl",
                table: "dispatches");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "dispatches");
        }
    }
}
