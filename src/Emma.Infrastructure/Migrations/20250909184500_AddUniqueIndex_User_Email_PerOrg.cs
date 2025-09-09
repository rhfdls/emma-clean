using Emma.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emma.Infrastructure.Migrations
{
    [DbContext(typeof(EmmaDbContext))]
    [Migration("20250909184500_AddUniqueIndex_User_Email_PerOrg")]
    public partial class AddUniqueIndex_User_Email_PerOrg : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Deduplicate existing users by (OrganizationId, lower(Email)) keeping the newest by CreatedAt
            migrationBuilder.Sql(@"
WITH ranked AS (
  SELECT ""Id"", ""OrganizationId"", lower(""Email"") AS email_norm,
         ROW_NUMBER() OVER (PARTITION BY ""OrganizationId"", lower(""Email"") ORDER BY ""CreatedAt"" DESC, ""Id"" DESC) AS rn
  FROM ""Users""
)
DELETE FROM ""Users"" u
USING ranked r
WHERE u.""Id"" = r.""Id"" AND r.rn > 1;
");

            // Create a unique index on (OrganizationId, lower(Email))
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS ux_users_org_lower_email ON \"Users\" (\"OrganizationId\", lower(\"Email\"));");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ux_users_org_lower_email;");
        }
    }
}
