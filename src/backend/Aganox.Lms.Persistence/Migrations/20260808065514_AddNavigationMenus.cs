using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aganox.Lms.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNavigationMenus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NavigationMenuItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LabelKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Route = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequiredPermissions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavigationMenuItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NavigationMenuItems_NavigationMenuItems_ParentId",
                        column: x => x.ParentId,
                        principalTable: "NavigationMenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenuItems_Code",
                table: "NavigationMenuItems",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenuItems_ParentId",
                table: "NavigationMenuItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenuItems_SortOrder",
                table: "NavigationMenuItems",
                column: "SortOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NavigationMenuItems");
        }
    }
}
