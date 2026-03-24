using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhoaLuanTotNghiep.Migrations
{
    /// <inheritdoc />
    public partial class AddZoneMonthly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ZoneVersions_Areas_AreaId",
                table: "ZoneVersions");

            migrationBuilder.AlterColumn<int>(
                name: "SoPolygon",
                table: "ZoneVersions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<double>(
                name: "DienTichBaoPhu",
                table: "ZoneVersions",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<int>(
                name: "AreaId",
                table: "ZoneVersions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<double>(
                name: "Area",
                table: "Zones",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "ZoneMonthlyDatas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ZoneId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    ExpectedOrders = table.Column<int>(type: "int", nullable: false),
                    ExpectedCustomers = table.Column<int>(type: "int", nullable: false),
                    ActualOrders = table.Column<int>(type: "int", nullable: false),
                    ActualCustomers = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZoneMonthlyDatas", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_ZoneVersions_Areas_AreaId",
                table: "ZoneVersions",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ZoneVersions_Areas_AreaId",
                table: "ZoneVersions");

            migrationBuilder.DropTable(
                name: "ZoneMonthlyDatas");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "Zones");

            migrationBuilder.AlterColumn<int>(
                name: "SoPolygon",
                table: "ZoneVersions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "DienTichBaoPhu",
                table: "ZoneVersions",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AreaId",
                table: "ZoneVersions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ZoneVersions_Areas_AreaId",
                table: "ZoneVersions",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
