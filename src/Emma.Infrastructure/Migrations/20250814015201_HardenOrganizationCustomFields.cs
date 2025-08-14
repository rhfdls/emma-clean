using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenOrganizationCustomFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"-- Ensure Organizations.CustomFields exists as jsonb with default and not null
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_name = 'Organizations' AND column_name = 'CustomFields'
    ) THEN
        -- Create the column with default and not null
        ALTER TABLE ""Organizations"" ADD COLUMN ""CustomFields"" jsonb NOT NULL DEFAULT '{}'::jsonb;
    ELSE
        -- Backfill nulls to empty jsonb
        UPDATE ""Organizations"" SET ""CustomFields"" = '{}'::jsonb WHERE ""CustomFields"" IS NULL;

        -- Set default '{}'::jsonb if not already set
        IF NOT EXISTS (
            SELECT 1
            FROM pg_attrdef d
            JOIN pg_class t ON t.oid = d.adrelid
            JOIN pg_attribute a ON a.attrelid = d.adrelid AND a.attnum = d.adnum
            WHERE t.relname = 'Organizations' AND a.attname = 'CustomFields'
        ) THEN
            ALTER TABLE ""Organizations"" ALTER COLUMN ""CustomFields"" SET DEFAULT '{}'::jsonb;
        END IF;

        -- Enforce NOT NULL
        ALTER TABLE ""Organizations"" ALTER COLUMN ""CustomFields"" SET NOT NULL;
    END IF;
END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"-- Allow NULLs again (schema-only)
ALTER TABLE ""Organizations"" ALTER COLUMN ""CustomFields"" DROP NOT NULL;

-- Drop default if present
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM pg_attrdef d
        JOIN pg_class t ON t.oid = d.adrelid
        JOIN pg_attribute a ON a.attrelid = d.adrelid AND a.attnum = d.adnum
        WHERE t.relname = 'Organizations' AND a.attname = 'CustomFields'
    ) THEN
        ALTER TABLE ""Organizations"" ALTER COLUMN ""CustomFields"" DROP DEFAULT;
    END IF;
END $$;");
        }
    }
}
