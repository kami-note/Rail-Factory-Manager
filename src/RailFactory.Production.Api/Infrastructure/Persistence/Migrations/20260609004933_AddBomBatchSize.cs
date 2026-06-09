using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Production.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBomBatchSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BatchSize",
                table: "boms",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 1.0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchSize",
                table: "boms");
        }
    }
}
