using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailFactory.Iam.Api.Infrastructure.Auth.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LegacyTenantCodeCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                do $$
                begin
                    if exists (
                        select 1
                        from information_schema.columns
                        where table_schema = 'public'
                          and table_name = 'iam_local_users'
                          and column_name = 'tenant_code'
                    ) then
                        alter table iam_local_users drop constraint if exists iam_local_users_pkey;
                        alter table iam_local_users drop constraint if exists "PK_iam_local_users";
                        drop index if exists ix_iam_local_users_tenant_email;
                        alter table iam_local_users drop column tenant_code;
                        alter table iam_local_users add primary key (external_provider, external_subject);
                    end if;
                end
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
