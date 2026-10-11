using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetDesk.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InvoiceCompanyAndBankDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankAccountName",
                table: "ClinicSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "ClinicSettings",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankSortCode",
                table: "ClinicSettings",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "ClinicSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyNumber",
                table: "ClinicSettings",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyRegisteredIn",
                table: "ClinicSettings",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegisteredOffice",
                table: "ClinicSettings",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccountName",
                table: "ClinicSettings");

            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "ClinicSettings");

            migrationBuilder.DropColumn(
                name: "BankSortCode",
                table: "ClinicSettings");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "ClinicSettings");

            migrationBuilder.DropColumn(
                name: "CompanyNumber",
                table: "ClinicSettings");

            migrationBuilder.DropColumn(
                name: "CompanyRegisteredIn",
                table: "ClinicSettings");

            migrationBuilder.DropColumn(
                name: "RegisteredOffice",
                table: "ClinicSettings");
        }
    }
}
