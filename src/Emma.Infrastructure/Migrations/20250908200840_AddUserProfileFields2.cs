using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Emma.Infrastructure.Data;

#nullable disable

namespace Emma.Infrastructure.Migrations
{
    /// <summary>
    /// Manual migration to add optional profile fields to Users (v2, later timestamp).
    /// Adds: Locale (text), TimeZone (text), ProfileImageUrl (text)
    /// </summary>
    [DbContext(typeof(EmmaDbContext))]
    [Migration("20250908235959_AddUserProfileFields")] // ensure this is after 20250908233638_AddUserLastLoginAt
    public partial class AddUserProfileFields2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Locale",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrl",
                table: "Users",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Locale",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TimeZone",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProfileImageUrl",
                table: "Users");
        }
    }
}
