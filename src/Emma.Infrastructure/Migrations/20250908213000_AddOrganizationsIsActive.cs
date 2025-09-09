using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Emma.Infrastructure.Data;

#nullable disable

namespace Emma.Infrastructure.Migrations
{
    [DbContext(typeof(EmmaDbContext))]
    [Migration("20250908213000_AddOrganizationsIsActive")]
    public partial class AddOrganizationsIsActive : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_IsActive",
                table: "Organizations",
                column: "IsActive");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Organizations_IsActive",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Organizations");
        }
    }
}
