using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Logistics.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantFiscalProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenant_fiscal_profile",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CfopPadraoIntraestadual = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    CfopPadraoInterestadual = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    UfOrigem = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    IcmsRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    IcmsCst = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PisCst = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CofinsCst = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    IpiRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    IcmsOrigin = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_fiscal_profile", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_fiscal_profile");
        }
    }
}
