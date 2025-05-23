using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Beltelecom.Migrations
{
    /// <inheritdoc />
    public partial class AddServicePackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ServicePackageId",
                table: "UserDealInfos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServicePackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Internet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneServices = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TVServices = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VideoMonitoring = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePackages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserDealInfos_ServicePackageId",
                table: "UserDealInfos",
                column: "ServicePackageId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserDealInfos_ServicePackages_ServicePackageId",
                table: "UserDealInfos",
                column: "ServicePackageId",
                principalTable: "ServicePackages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserDealInfos_ServicePackages_ServicePackageId",
                table: "UserDealInfos");

            migrationBuilder.DropTable(
                name: "ServicePackages");

            migrationBuilder.DropIndex(
                name: "IX_UserDealInfos_ServicePackageId",
                table: "UserDealInfos");

            migrationBuilder.DropColumn(
                name: "ServicePackageId",
                table: "UserDealInfos");
        }
    }
}
