using Microsoft.EntityFrameworkCore;
using KhoaLuanTotNghiep.Models;

namespace KhoaLuanTotNghiep.Data

{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        { 
        }
        public DbSet<TaiKhoan> TaiKhoans { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Area> Areas { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<ZoneVersion> ZoneVersions { get; set; }
        public DbSet<Zone> Zones { get; set; }
        public DbSet<ZoneStatistic> ZoneStatistics { get; set; }
        public DbSet<ZoneDistance> ZoneDistances { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<ZoneMonthlyData> ZoneMonthlyDatas { get; set; }
        public DbSet<ZoneHistory> ZoneHistories { get; set; }
        public DbSet<ZoneRoute> ZoneRoutes { get; set; }
    }   
}

