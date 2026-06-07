using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Fleet.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleRntrc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Rntrc",
                table: "vehicles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rntrc",
                table: "vehicles");
        }
    }
}
