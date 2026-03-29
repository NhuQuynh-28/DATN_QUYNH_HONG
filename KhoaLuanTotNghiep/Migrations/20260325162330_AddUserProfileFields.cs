using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhoaLuanTotNghiep.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thêm các cột mới vào bảng Users (chỉ thêm nếu chưa tồn tại)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Address')
                    ALTER TABLE Users ADD Address nvarchar(max) NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Cccd')
                    ALTER TABLE Users ADD Cccd nvarchar(max) NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Phone')
                    ALTER TABLE Users ADD Phone nvarchar(max) NULL;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'CreatedAt')
                    ALTER TABLE Users ADD CreatedAt datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.000';
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'UpdatedAt')
                    ALTER TABLE Users ADD UpdatedAt datetime2 NULL;
            ");

            // Cập nhật các cột ZoneMonthlyDatas nếu cần
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ZoneMonthlyDatas' AND COLUMN_NAME = 'ActualCustomers')
                    ALTER TABLE ZoneMonthlyDatas DROP COLUMN ActualCustomers;
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ZoneMonthlyDatas' AND COLUMN_NAME = 'ActualOrders')
                    ALTER TABLE ZoneMonthlyDatas DROP COLUMN ActualOrders;
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ZoneMonthlyDatas' AND COLUMN_NAME = 'ExpectedCustomers')
                    ALTER TABLE ZoneMonthlyDatas DROP COLUMN ExpectedCustomers;
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ZoneMonthlyDatas' AND COLUMN_NAME = 'ExpectedOrders')
                    ALTER TABLE ZoneMonthlyDatas DROP COLUMN ExpectedOrders;
            ");
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ZoneMonthlyDatas' AND COLUMN_NAME = 'CustomersForecast')
                    ALTER TABLE ZoneMonthlyDatas ADD CustomersForecast int NULL;
            ");
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ZoneMonthlyDatas' AND COLUMN_NAME = 'CustomersReal')
                    ALTER TABLE ZoneMonthlyDatas ADD CustomersReal int NULL;
            ");
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ZoneMonthlyDatas' AND COLUMN_NAME = 'OrdersForecast')
                    ALTER TABLE ZoneMonthlyDatas ADD OrdersForecast int NULL;
            ");
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ZoneMonthlyDatas' AND COLUMN_NAME = 'OrdersReal')
                    ALTER TABLE ZoneMonthlyDatas ADD OrdersReal int NULL;
            ");

            // Tạo ZoneHistories nếu chưa tồn tại
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ZoneHistories')
                BEGIN
                    CREATE TABLE ZoneHistories (
                        Id int NOT NULL IDENTITY(1,1),
                        ZoneId int NOT NULL,
                        Month int NOT NULL,
                        Year int NOT NULL,
                        OrdersReal int NULL,
                        CustomersReal int NULL,
                        OrdersForecast int NOT NULL DEFAULT 0,
                        CustomersForecast int NOT NULL DEFAULT 0,
                        CONSTRAINT PK_ZoneHistories PRIMARY KEY (Id),
                        CONSTRAINT FK_ZoneHistories_Zones_ZoneId FOREIGN KEY (ZoneId) REFERENCES Zones(Id) ON DELETE CASCADE
                    );
                    CREATE INDEX IX_ZoneHistories_ZoneId ON ZoneHistories(ZoneId);
                END
            ");

            // Tạo ZoneRoutes nếu chưa tồn tại
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ZoneRoutes')
                BEGIN
                    CREATE TABLE ZoneRoutes (
                        Id int NOT NULL IDENTITY(1,1),
                        ZoneId int NOT NULL,
                        Month int NOT NULL,
                        Year int NOT NULL,
                        Lat float NOT NULL DEFAULT 0,
                        Lng float NOT NULL DEFAULT 0,
                        CONSTRAINT PK_ZoneRoutes PRIMARY KEY (Id)
                    );
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Address') ALTER TABLE Users DROP COLUMN Address;");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Cccd') ALTER TABLE Users DROP COLUMN Cccd;");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Phone') ALTER TABLE Users DROP COLUMN Phone;");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'CreatedAt') ALTER TABLE Users DROP COLUMN CreatedAt;");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'UpdatedAt') ALTER TABLE Users DROP COLUMN UpdatedAt;");
        }
    }
}
