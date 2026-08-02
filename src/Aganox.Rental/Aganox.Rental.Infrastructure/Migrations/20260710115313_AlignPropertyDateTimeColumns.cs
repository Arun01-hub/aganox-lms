using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aganox.Rental.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignPropertyDateTimeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Properties_UpdatedAt",
                table: "Properties",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Properties_UpdatedAt",
                table: "Properties");
        }
    }
}
