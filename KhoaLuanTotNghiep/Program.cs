using KhoaLuanTotNghiep.Data;
using KhoaLuanTotNghiep.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSession();
// Add services
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();
//  ?? load ?nh, css, js
app.UseStaticFiles();
app.UseSession();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=AuthHome}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!context.ZoneHistories.Any())
    {
        var testZoneHistories = new List<ZoneHistory>();

        // Tạo dữ liệu 12 tháng cho 3 zone
        for (int zoneId = 1; zoneId <= 3; zoneId++)
        {
            for (int month = 1; month <= 12; month++)
            {
                testZoneHistories.Add(new ZoneHistory
                {
                    ZoneId = zoneId,
                    Month = month,
                    Year = 2026,
                    OrdersReal = 50 + zoneId * 10 + month * 5,       // giá trị giả
                    CustomersReal = 30 + zoneId * 5 + month * 3,
                    OrdersForecast = 60 + zoneId * 10 + month * 6,
                    CustomersForecast = 40 + zoneId * 5 + month * 4
                });
            }
        }

        context.ZoneHistories.AddRange(testZoneHistories);
        context.SaveChanges();
    }
}
app.Run();