using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhoaLuanTotNghiep.Migrations
{
    /// <inheritdoc />
    public partial class UpdateZoneInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AvgPerDay",
                table: "Zones",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AvgPerWeek",
                table: "Zones",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Customers",
                table: "Zones",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Zones",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvgPerDay",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "AvgPerWeek",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "Customers",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "Zones");
        }
    }
}
