using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Repairs legacy supplier mappings that received an empty InternalUnitOfMeasure
    /// after the column was introduced.
    /// </summary>
    public partial class BackfillSupplierMappingInternalUnit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE supplier_material_mappings smm
                SET "InternalUnitOfMeasure" = src."UnitOfMeasure"
                FROM (
                    SELECT DISTINCT ON (s."FiscalId", mri."SupplierProductCode")
                        s."FiscalId" AS "SupplierFiscalId",
                        mri."SupplierProductCode",
                        mri."UnitOfMeasure"
                    FROM material_receipt_items mri
                    INNER JOIN material_receipts mr ON mr."Id" = mri."ReceiptId"
                    INNER JOIN suppliers s ON s."Id" = mr."SupplierId"
                    WHERE mri."AssociationStatus" IN ('Mapped', 'CreatedAndMapped')
                      AND mri."UnitOfMeasure" IS NOT NULL
                      AND BTRIM(mri."UnitOfMeasure") <> ''
                    ORDER BY s."FiscalId", mri."SupplierProductCode", mri."AssociationUpdatedAt" DESC
                ) src
                WHERE BTRIM(smm."InternalUnitOfMeasure") = ''
                  AND smm."SupplierFiscalId" = src."SupplierFiscalId"
                  AND smm."SupplierProductCode" = src."SupplierProductCode";
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No down data operation: this migration only repairs missing historical values.
        }
    }
}
