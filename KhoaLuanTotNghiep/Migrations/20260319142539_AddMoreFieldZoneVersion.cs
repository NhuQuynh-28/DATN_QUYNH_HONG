using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhoaLuanTotNghiep.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreFieldZoneVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VersionId",
                table: "Zones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AreaId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ResetCode",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Areas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Areas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Drivers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DriverName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drivers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZoneVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AreaId = table.Column<int>(type: "int", nullable: false),
                    VersionName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SoPolygon = table.Column<int>(type: "int", nullable: false),
                    DienTichBaoPhu = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZoneVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZoneVersions_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DriverId = table.Column<int>(type: "int", nullable: false),
                    DispatcherId = table.Column<int>(type: "int", nullable: false),
                    AreaId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlannedOrders = table.Column<int>(type: "int", nullable: false),
                    ActualOrders = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assignments_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Assignments_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Assignments_Users_DispatcherId",
                        column: x => x.DispatcherId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Zones_VersionId",
                table: "Zones",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_AreaId",
                table: "Users",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_AreaId",
                table: "Assignments",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_DispatcherId",
                table: "Assignments",
                column: "DispatcherId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_DriverId",
                table: "Assignments",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_ZoneVersions_AreaId",
                table: "ZoneVersions",
                column: "AreaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Areas_AreaId",
                table: "Users",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Zones_ZoneVersions_VersionId",
                table: "Zones",
                column: "VersionId",
                principalTable: "ZoneVersions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Areas_AreaId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Zones_ZoneVersions_VersionId",
                table: "Zones");

            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.DropTable(
                name: "ZoneVersions");

            migrationBuilder.DropTable(
                name: "Drivers");

            migrationBuilder.DropTable(
                name: "Areas");

            migrationBuilder.DropIndex(
                name: "IX_Zones_VersionId",
                table: "Zones");

            migrationBuilder.DropIndex(
                name: "IX_Users_AreaId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "VersionId",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "AreaId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResetCode",
                table: "Users");
        }
    }
}
