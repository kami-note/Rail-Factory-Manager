using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supplier_material_mappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierFiscalId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SupplierProductCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InternalMaterialCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SupplierUnit = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_material_mappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_material_mappings_SupplierFiscalId_SupplierProduct~",
                table: "supplier_material_mappings",
                columns: new[] { "SupplierFiscalId", "SupplierProductCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supplier_material_mappings");
        }
    }
}
