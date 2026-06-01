using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Tenancy.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenant_integrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "varchar(50)", nullable: false),
                    provider_type = table.Column<string>(type: "varchar(50)", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    encrypted_credentials = table.Column<byte[]>(type: "bytea", nullable: false),
                    credentials_dek = table.Column<byte[]>(type: "bytea", nullable: false),
                    credentials_iv = table.Column<byte[]>(type: "bytea", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_integrations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "uix_tenant_integrations_tenant_category",
                table: "tenant_integrations",
                columns: new[] { "tenant_id", "category" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_integrations");
        }
    }
}
