using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalEnvironmentToReceipt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FiscalEnvironment",
                table: "material_receipts",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FiscalEnvironment",
                table: "material_receipts");
        }
    }
}
