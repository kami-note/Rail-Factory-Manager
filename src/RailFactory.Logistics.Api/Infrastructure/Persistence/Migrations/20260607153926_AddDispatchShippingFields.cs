using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Logistics.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchShippingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShippingErrorMessage",
                table: "dispatches",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingExternalId",
                table: "dispatches",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingLabelUrl",
                table: "dispatches",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingStatus",
                table: "dispatches",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_dispatches_ShippingExternalId",
                table: "dispatches",
                column: "ShippingExternalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_dispatches_ShippingExternalId",
                table: "dispatches");

            migrationBuilder.DropColumn(
                name: "ShippingErrorMessage",
                table: "dispatches");

            migrationBuilder.DropColumn(
                name: "ShippingExternalId",
                table: "dispatches");

            migrationBuilder.DropColumn(
                name: "ShippingLabelUrl",
                table: "dispatches");

            migrationBuilder.DropColumn(
                name: "ShippingStatus",
                table: "dispatches");
        }
    }
}
