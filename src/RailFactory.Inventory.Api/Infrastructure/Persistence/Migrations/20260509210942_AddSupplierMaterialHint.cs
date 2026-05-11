using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Inventory.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierMaterialHint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supplier_material_hints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierFiscalId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SupplierProductCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    MappedMaterialCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_material_hints", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_material_hints_SupplierFiscalId_SupplierProductCode",
                table: "supplier_material_hints",
                columns: new[] { "SupplierFiscalId", "SupplierProductCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supplier_material_hints");
        }
    }
}
