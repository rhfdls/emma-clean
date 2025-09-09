using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sync_User_Model_2025Q3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<JsonDocument>(
                name: "analysis_json",
                table: "Interactions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<JsonDocument>(
                name: "analysis_run_log_json",
                table: "Interactions",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "analysis_json",
                table: "Interactions");

            migrationBuilder.DropColumn(
                name: "analysis_run_log_json",
                table: "Interactions");
        }
    }
}
