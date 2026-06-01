using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Iam.Api.Infrastructure.Auth.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMfaTotp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "mfa_enabled",
                table: "iam_local_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "mfa_enabled_at",
                table: "iam_local_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mfa_secret_base32",
                table: "iam_local_users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mfa_enabled",
                table: "iam_local_users");

            migrationBuilder.DropColumn(
                name: "mfa_enabled_at",
                table: "iam_local_users");

            migrationBuilder.DropColumn(
                name: "mfa_secret_base32",
                table: "iam_local_users");
        }
    }
}
