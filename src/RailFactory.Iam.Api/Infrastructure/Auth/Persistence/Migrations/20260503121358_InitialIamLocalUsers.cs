using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Iam.Api.Infrastructure.Auth.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialIamLocalUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "iam_local_users",
                columns: table => new
                {
                    external_provider = table.Column<string>(type: "text", nullable: false),
                    external_subject = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    first_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_local_users", x => new { x.external_provider, x.external_subject });
                });

            migrationBuilder.CreateIndex(
                name: "ix_iam_local_users_email",
                table: "iam_local_users",
                column: "email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "iam_local_users");
        }
    }
}
