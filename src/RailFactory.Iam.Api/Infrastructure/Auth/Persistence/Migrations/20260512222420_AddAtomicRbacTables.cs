using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Iam.Api.Infrastructure.Auth.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAtomicRbacTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "iam_tenant_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    permissions = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_tenant_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "iam_tenant_user_roles",
                columns: table => new
                {
                    tenant_code = table.Column<string>(type: "text", nullable: false),
                    external_provider = table.Column<string>(type: "text", nullable: false),
                    external_subject = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_tenant_user_roles", x => new { x.tenant_code, x.external_provider, x.external_subject, x.role_id });
                    table.ForeignKey(
                        name: "FK_iam_tenant_user_roles_iam_local_users_external_provider_ext~",
                        columns: x => new { x.external_provider, x.external_subject },
                        principalTable: "iam_local_users",
                        principalColumns: new[] { "external_provider", "external_subject" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_iam_tenant_user_roles_iam_tenant_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "iam_tenant_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_iam_tenant_roles_tenant_code",
                table: "iam_tenant_roles",
                column: "tenant_code");

            migrationBuilder.CreateIndex(
                name: "IX_iam_tenant_user_roles_external_provider_external_subject",
                table: "iam_tenant_user_roles",
                columns: new[] { "external_provider", "external_subject" });

            migrationBuilder.CreateIndex(
                name: "IX_iam_tenant_user_roles_role_id",
                table: "iam_tenant_user_roles",
                column: "role_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "iam_tenant_user_roles");

            migrationBuilder.DropTable(
                name: "iam_tenant_roles");
        }
    }
}
