using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Iam.Api.Infrastructure.Auth.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeRoleSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "child_role_ids",
                table: "iam_tenant_roles",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "child_role_ids",
                table: "iam_tenant_roles");
        }
    }
}
