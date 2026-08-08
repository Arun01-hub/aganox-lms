using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aganox.Lms.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollSalaryEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CalculatedSnapshotJson",
                table: "PayrollEmployeeResults",
                type: "nvarchar(max)",
                maxLength: 16000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsManuallyAdjusted",
                table: "PayrollEmployeeResults",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalculatedSnapshotJson",
                table: "PayrollEmployeeResults");

            migrationBuilder.DropColumn(
                name: "IsManuallyAdjusted",
                table: "PayrollEmployeeResults");
        }
    }
}
