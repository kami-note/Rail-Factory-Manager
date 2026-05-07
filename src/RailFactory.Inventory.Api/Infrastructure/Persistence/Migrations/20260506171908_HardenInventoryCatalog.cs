using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Inventory.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenInventoryCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "materials",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "materials");
        }
    }
}
