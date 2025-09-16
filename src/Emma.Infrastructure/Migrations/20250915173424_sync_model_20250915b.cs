using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class sync_model_20250915b : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmailAddresses_Contacts_ContactId1",
                table: "EmailAddresses");

            migrationBuilder.DropColumn(
                name: "DeletedById",
                table: "Contacts");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedById",
                table: "Interactions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ContactId1",
                table: "EmailAddresses",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Contacts",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailAddresses_Contacts_ContactId1",
                table: "EmailAddresses",
                column: "ContactId1",
                principalTable: "Contacts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmailAddresses_Contacts_ContactId1",
                table: "EmailAddresses");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedById",
                table: "Interactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ContactId1",
                table: "EmailAddresses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Contacts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedById",
                table: "Contacts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EmailAddresses_Contacts_ContactId1",
                table: "EmailAddresses",
                column: "ContactId1",
                principalTable: "Contacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
