using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emma.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessAuditLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestingAgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccessGranted = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PrivacyTags = table.Column<string>(type: "jsonb", maxLength: 1000, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessAuditLogs_Agents_RequestingAgentId",
                        column: x => x.RequestingAgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccessAuditLogs_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AccessAuditLogs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessAuditLogs_AccessedAt",
                table: "AccessAuditLogs",
                column: "AccessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AccessAuditLogs_ContactId",
                table: "AccessAuditLogs",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessAuditLogs_OrganizationId",
                table: "AccessAuditLogs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessAuditLogs_RequestingAgentId",
                table: "AccessAuditLogs",
                column: "RequestingAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessAuditLogs_ResourceType",
                table: "AccessAuditLogs",
                column: "ResourceType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessAuditLogs");
        }
    }
}
