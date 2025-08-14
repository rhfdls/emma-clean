using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforcePlanIdNotNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Backfill (idempotent)
            migrationBuilder.Sql(
                @"UPDATE ""Organizations""
                  SET ""PlanId"" = COALESCE(""PlanId"", 'free')
                  WHERE ""PlanId"" IS NULL;");

            // 2) (Optional) Set a default for new rows
            migrationBuilder.Sql(
                @"ALTER TABLE ""Organizations""
                  ALTER COLUMN ""PlanId"" SET DEFAULT 'free';");

            // 3) Enforce NOT NULL
            migrationBuilder.Sql(
                @"ALTER TABLE ""Organizations""
                  ALTER COLUMN ""PlanId"" SET NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Allow NULL again
            migrationBuilder.Sql(
                @"ALTER TABLE ""Organizations""
                  ALTER COLUMN ""PlanId"" DROP NOT NULL;");

            // Drop the default
            migrationBuilder.Sql(
                @"ALTER TABLE ""Organizations""
                  ALTER COLUMN ""PlanId"" DROP DEFAULT;");
        }
    }
}
