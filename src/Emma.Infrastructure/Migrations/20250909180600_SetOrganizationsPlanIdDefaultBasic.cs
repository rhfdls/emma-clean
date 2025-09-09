using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Emma.Infrastructure.Data;

#nullable disable

namespace Emma.Infrastructure.Migrations
{
    [DbContext(typeof(EmmaDbContext))]
    [Migration("20250909180600_SetOrganizationsPlanIdDefaultBasic")]
    public partial class SetOrganizationsPlanIdDefaultBasic : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill any NULL PlanId with 'Basic' to satisfy NOT NULL/default assumptions
            migrationBuilder.Sql("UPDATE \"Organizations\" SET \"PlanId\" = 'Basic' WHERE \"PlanId\" IS NULL;");

            // Set a DB-level default so future inserts without PlanId receive 'Basic'
            migrationBuilder.Sql("ALTER TABLE \"Organizations\" ALTER COLUMN \"PlanId\" SET DEFAULT 'Basic';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the default; keep already set values intact
            migrationBuilder.Sql("ALTER TABLE \"Organizations\" ALTER COLUMN \"PlanId\" DROP DEFAULT;");
        }
    }
}
