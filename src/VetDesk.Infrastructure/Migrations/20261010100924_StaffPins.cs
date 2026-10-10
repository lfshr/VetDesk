using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetDesk.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StaffPins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StaffMembers_Email",
                table: "StaffMembers");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "StaffMembers",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(400)",
                oldMaxLength: 400);

            migrationBuilder.AddColumn<int>(
                name: "FailedPinAttempts",
                table: "StaffMembers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PinHash",
                table: "StaffMembers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PinLockedUntilUtc",
                table: "StaffMembers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembers_Email",
                table: "StaffMembers",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StaffMembers_Email",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "FailedPinAttempts",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "PinHash",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "PinLockedUntilUtc",
                table: "StaffMembers");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "StaffMembers",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(400)",
                oldMaxLength: 400,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembers_Email",
                table: "StaffMembers",
                column: "Email",
                unique: true);
        }
    }
}
