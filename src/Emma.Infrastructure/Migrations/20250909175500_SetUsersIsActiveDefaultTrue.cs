using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Emma.Infrastructure.Data;

#nullable disable

namespace Emma.Infrastructure.Migrations
{
    [DbContext(typeof(EmmaDbContext))]
    [Migration("20250909175500_SetUsersIsActiveDefaultTrue")]
    public partial class SetUsersIsActiveDefaultTrue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure any existing NULLs are set to TRUE to satisfy NOT NULL
            migrationBuilder.Sql("UPDATE \"Users\" SET \"IsActive\" = TRUE WHERE \"IsActive\" IS NULL;");

            // Ensure the column has a default of TRUE for future inserts
            migrationBuilder.Sql("ALTER TABLE \"Users\" ALTER COLUMN \"IsActive\" SET DEFAULT TRUE;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert the default; keep the existing TRUE values as-is
            migrationBuilder.Sql("ALTER TABLE \"Users\" ALTER COLUMN \"IsActive\" DROP DEFAULT;");
        }
    }
}
