using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationPlanId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add PlanId (nullable)
            migrationBuilder.AddColumn<string>(
                name: "PlanId",
                table: "Organizations",
                type: "text",
                nullable: true);

            // Add VerifiedAt idempotently (use raw SQL instead of AddColumn if this may already exist)
            migrationBuilder.Sql(
                @"ALTER TABLE ""Users""
                  ADD COLUMN IF NOT EXISTS ""VerifiedAt"" timestamptz NULL;");

            // Create index idempotently (avoid duplicate index errors)
            migrationBuilder.Sql(
                @"CREATE INDEX IF NOT EXISTS ""IX_Organizations_PlanId""
                  ON ""Organizations"" (""PlanId"");");

            // Backfill PlanId from integer PlanType -> string names
            migrationBuilder.Sql(
                @"UPDATE ""Organizations""
                    SET ""PlanId"" = CASE ""PlanType""
                        WHEN 0 THEN 'Free'
                        WHEN 1 THEN 'Basic'
                        WHEN 2 THEN 'Pro'
                        WHEN 3 THEN 'Enterprise'
                        ELSE ""PlanId""
                    END
                  WHERE ""PlanId"" IS NULL
                    AND ""PlanType"" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Idempotent drop of index if it exists
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Organizations_PlanId"";");

            migrationBuilder.DropColumn(
                name: "PlanId",
                table: "Organizations");

            // Typically we do not remove VerifiedAt in Down() to preserve audit history; leaving as-is.
        }
    }
}
