using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Logistics.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDamdfeDisplayFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmitterCity",
                table: "tenant_fiscal_profile",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmitterCnpj",
                table: "tenant_fiscal_profile",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmitterIe",
                table: "tenant_fiscal_profile",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmitterName",
                table: "tenant_fiscal_profile",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmitterState",
                table: "tenant_fiscal_profile",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MdfeUfCarregamento",
                table: "dispatches",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MdfeUfDescarregamento",
                table: "dispatches",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmitterCity",
                table: "tenant_fiscal_profile");

            migrationBuilder.DropColumn(
                name: "EmitterCnpj",
                table: "tenant_fiscal_profile");

            migrationBuilder.DropColumn(
                name: "EmitterIe",
                table: "tenant_fiscal_profile");

            migrationBuilder.DropColumn(
                name: "EmitterName",
                table: "tenant_fiscal_profile");

            migrationBuilder.DropColumn(
                name: "EmitterState",
                table: "tenant_fiscal_profile");

            migrationBuilder.DropColumn(
                name: "MdfeUfCarregamento",
                table: "dispatches");

            migrationBuilder.DropColumn(
                name: "MdfeUfDescarregamento",
                table: "dispatches");
        }
    }
}
