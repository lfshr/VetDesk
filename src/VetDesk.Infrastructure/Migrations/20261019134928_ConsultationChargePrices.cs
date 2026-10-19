using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetDesk.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConsultationChargePrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "ConsultationCharges",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<decimal>(
                name: "ListUnitPrice",
                table: "ConsultationCharges",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceChangeReason",
                table: "ConsultationCharges",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceChangedBy",
                table: "ConsultationCharges",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ListUnitPrice",
                table: "ConsultationCharges");

            migrationBuilder.DropColumn(
                name: "PriceChangeReason",
                table: "ConsultationCharges");

            migrationBuilder.DropColumn(
                name: "PriceChangedBy",
                table: "ConsultationCharges");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "ConsultationCharges",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
