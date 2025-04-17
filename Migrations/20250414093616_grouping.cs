using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Beltelecom.Migrations
{
    /// <inheritdoc />
    public partial class grouping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Group",
                table: "ProductSpecifications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Group",
                table: "ProductSpecifications");
        }
    }
}
