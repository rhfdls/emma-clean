using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emma.Data.Migrations
{
    /// <summary>
    /// Adds a PostgreSQL trigger to auto-generate RowVersion (bytea) for Organizations on insert and update.
    /// </summary>
    public partial class AddRowVersionTriggerToOrganizations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create a function to generate a new random 8-byte RowVersion
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION set_rowversion_organizations()
RETURNS TRIGGER AS $$
BEGIN
    NEW.""RowVersion"" := gen_random_bytes(8);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
");

            // Create the trigger for insert and update
            migrationBuilder.Sql(@"
DROP TRIGGER IF EXISTS organizations_rowversion_trigger ON ""Organizations"";
CREATE TRIGGER organizations_rowversion_trigger
BEFORE INSERT OR UPDATE ON ""Organizations""
FOR EACH ROW EXECUTE FUNCTION set_rowversion_organizations();
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TRIGGER IF EXISTS organizations_rowversion_trigger ON ""Organizations"";
DROP FUNCTION IF EXISTS set_rowversion_organizations();
");
        }
    }
}
