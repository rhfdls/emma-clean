using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S2_AddArchiveAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Contacts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Contacts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Contacts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TraceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DetailsJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_OrganizationId_IsArchived_OwnerId",
                table: "Contacts",
                columns: new[] { "OrganizationId", "IsArchived", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Action",
                table: "AuditEvents",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_OccurredAt",
                table: "AuditEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_OrganizationId",
                table: "AuditEvents",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropIndex(
                name: "IX_Contacts_OrganizationId_IsArchived_OwnerId",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Contacts");
        }
    }
}
