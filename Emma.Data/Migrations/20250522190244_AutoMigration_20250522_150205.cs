using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emma.Data.Migrations
{
    /// <inheritdoc />
    public partial class AutoMigration_20250522_150205 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiResponse",
                table: "Messages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiResponse",
                table: "Messages");
        }
    }
}
