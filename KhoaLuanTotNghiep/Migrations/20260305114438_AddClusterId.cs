using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhoaLuanTotNghiep.Migrations
{
    /// <inheritdoc />
    public partial class AddClusterId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClusterId",
                table: "Zones",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClusterId",
                table: "Zones");
        }
    }
}
