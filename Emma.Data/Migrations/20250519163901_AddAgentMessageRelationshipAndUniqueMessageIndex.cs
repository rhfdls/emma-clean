using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emma.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentMessageRelationshipAndUniqueMessageIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_OccurredAt",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_Type",
                table: "Messages");

            migrationBuilder.AddColumn<Guid>(
                name: "AgentId",
                table: "Messages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Messages_AgentId",
                table: "Messages",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_OccurredAt_Type",
                table: "Messages",
                columns: new[] { "OccurredAt", "Type" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Agents_AgentId",
                table: "Messages",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Agents_AgentId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_AgentId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_OccurredAt_Type",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "AgentId",
                table: "Messages");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_OccurredAt",
                table: "Messages",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_Type",
                table: "Messages",
                column: "Type");
        }
    }
}
