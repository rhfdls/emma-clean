using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersVerifiedConsistency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Normalize existing data to satisfy the constraint
            migrationBuilder.Sql(@"-- Ensure IsVerified is never NULL
UPDATE ""Users"" SET ""IsVerified"" = FALSE WHERE ""IsVerified"" IS NULL;

-- If user marked verified but missing timestamp, set VerifiedAt to now
UPDATE ""Users"" SET ""VerifiedAt"" = NOW() AT TIME ZONE 'UTC' WHERE ""IsVerified"" = TRUE AND ""VerifiedAt"" IS NULL;

-- If user not verified but has a timestamp, null it out
UPDATE ""Users"" SET ""VerifiedAt"" = NULL WHERE ""IsVerified"" = FALSE AND ""VerifiedAt"" IS NOT NULL;

-- Add check constraint idempotently
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint c
        JOIN pg_class t ON t.oid = c.conrelid
        WHERE c.conname = 'users_verified_consistency'
          AND t.relname = 'Users'
    ) THEN
        ALTER TABLE ""Users""
        ADD CONSTRAINT users_verified_consistency
        CHECK ( (""IsVerified"" = FALSE AND ""VerifiedAt"" IS NULL)
             OR (""IsVerified"" = TRUE  AND ""VerifiedAt"" IS NOT NULL) );
    END IF;
END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM pg_constraint c
        JOIN pg_class t ON t.oid = c.conrelid
        WHERE c.conname = 'users_verified_consistency'
          AND t.relname = 'Users'
    ) THEN
        ALTER TABLE ""Users"" DROP CONSTRAINT users_verified_consistency;
    END IF;
END $$;");
        }
    }
}
