using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetDesk.Infrastructure.Migrations
{
    /// <summary>
    /// Replaces the four SOAP sections with clinical notes and advice for the owner. The SOAP text is dropped, not
    /// carried over: there is only demo data so far, and EF's guessed renames would have put the owner's history
    /// (Subjective) into the advice printed for owners.
    /// </summary>
    public partial class SimpleClinicalNotes : Migration
    {
        private static readonly string[] Soap = ["Subjective", "Objective", "Assessment", "Plan"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var column in Soap)
                migrationBuilder.DropColumn(name: column, table: "Consultations");

            migrationBuilder.AddColumn<string>(name: "Notes", table: "Consultations", type: "nvarchar(max)", maxLength: 8000, nullable: true);
            migrationBuilder.AddColumn<string>(name: "OwnerAdvice", table: "Consultations", type: "nvarchar(max)", maxLength: 8000, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Notes", table: "Consultations");
            migrationBuilder.DropColumn(name: "OwnerAdvice", table: "Consultations");

            foreach (var column in Soap)
                migrationBuilder.AddColumn<string>(name: column, table: "Consultations", type: "nvarchar(max)", maxLength: 8000, nullable: true);
        }
    }
}
