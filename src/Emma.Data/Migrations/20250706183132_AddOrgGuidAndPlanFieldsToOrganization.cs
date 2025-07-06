using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emma.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgGuidAndPlanFieldsToOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            

            migrationBuilder.AddColumn<int>(
                name: "AccountStatus",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VerificationToken",
                table: "Users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<Dictionary<string, string>>(
                name: "CustomFields",
                table: "ResourceRecommendations",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(Dictionary<string, string>),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrgGuid",
                table: "Organizations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "PlanType",
                table: "Organizations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeatCount",
                table: "Organizations",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<Dictionary<string, string>>(
                name: "CustomFields",
                table: "ContactAssignments",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(Dictionary<string, string>),
                oldType: "jsonb",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "VerificationToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OrgGuid",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "PlanType",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "SeatCount",
                table: "Organizations");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.AlterColumn<Dictionary<string, string>>(
                name: "CustomFields",
                table: "ResourceRecommendations",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(Dictionary<string, string>),
                oldType: "hstore",
                oldNullable: true);

            migrationBuilder.AlterColumn<Dictionary<string, string>>(
                name: "CustomFields",
                table: "ContactAssignments",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(Dictionary<string, string>),
                oldType: "hstore",
                oldNullable: true);
        }
    }
}
