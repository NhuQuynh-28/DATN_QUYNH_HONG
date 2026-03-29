using KhoaLuanTotNghiep.Data;
using KhoaLuanTotNghiep.Models;
using KhoaLuanTotNghiep.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSession();
builder.Services.AddMemoryCache();
// Add services
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ForecastingService>();
builder.Services.AddSingleton<HeuristicAssignmentService>();

var app = builder.Build();
//  ?? load ?nh, css, js
app.UseStaticFiles();

// Ngăn trình duyệt cache trang Login (bfcache fix)
// Khi Back button được bấm, browser sẽ request lại thay vì dùng cache
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    // Áp dụng no-cache cho trang Login, Logout và AuthHome
    if (path.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("/Account/Logout", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("/Home/AuthHome", StringComparison.OrdinalIgnoreCase) ||
        path == "/" || path == "")
    {
        context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        context.Response.Headers["Pragma"]        = "no-cache";
        context.Response.Headers["Expires"]       = "0";
    }
    await next();
});

app.UseSession();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=AuthHome}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();


    var missingMigrations = new[]
    {
        ("20260319142539_AddMoreFieldZoneVersion", "8.0.0"),
        ("20260320140007_AddZoneMonthly",           "8.0.0")
    };
    foreach (var (id, ver) in missingMigrations)
    {
        var exists = context.Database.ExecuteSqlRaw(
            $"IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '{id}') " +
            $"INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('{id}', '{ver}')");
    }

    // Tự động apply tất cả pending migrations khi start app
    context.Database.Migrate();

    if (!context.ZoneHistories.Any())
    {
        var testZoneHistories = new List<ZoneHistory>();

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

    // ── Dọn dẹp và Seed tài khoản mặc định ──────────────────────────────────
    static string Sha256(string input){
        var bytes = System.Security.Cryptography.SHA256.Create()
                        .ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower();
    }

    string adminHash = Sha256("Admin@123");
    
    // Tìm tất cả user có tên 'admin' (không phân biệt hoa thường)
    var admins = context.Users.Where(u => u.Username.ToLower() == "admin").ToList();

    if (!admins.Any())
    {
        // Nếu chưa có thì tạo mới
        context.Users.Add(new KhoaLuanTotNghiep.Models.User
        {
            Username   = "admin",
            Password   = adminHash,
            Email      = "admin@deliveryzone.vn",
            Role       = "Admin",
            IsApproved = true,
            IsActive   = true,
            CreatedAt  = DateTime.Now,
            UpdatedAt  = DateTime.Now
        });
        Console.WriteLine("🚀 Đã TẠO MỚI tài khoản Admin: admin / Admin@123");
    }
    else
    {
        // Nếu đã có, cập nhật TẤT CẢ các user tên 'admin' về cùng mật khẩu và quyền Admin
        foreach(var a in admins)
        {
            a.Password   = adminHash;
            a.Role       = "Admin";
            a.IsApproved = true;
            a.IsActive   = true;
            a.UpdatedAt  = DateTime.Now;
        }
        Console.WriteLine($"🚀 Đã CẬP NHẬT {admins.Count} tài khoản tên 'admin' về mật khẩu Admin@123");
    }
    context.SaveChanges();

    // Đảm bảo có Dispatcher để test
    if (!context.Users.Any(u => u.Username == "dispatcher"))
    {
        context.Users.Add(new KhoaLuanTotNghiep.Models.User
        {
            Username   = "dispatcher",
            Password   = Sha256("Dispatcher@123"),
            Email      = "dispatcher@deliveryzone.vn",
            Role       = "Dispatcher",
            IsApproved = true,
            IsActive   = true,
            CreatedAt  = DateTime.Now,
            UpdatedAt  = DateTime.Now
        });

        // Đảm bảo có Driver record khớp với driver2
        var dr2 = await context.Users.FirstOrDefaultAsync(u => u.Username == "driver2");
        if (dr2 != null)
        {
            var driverProfile = await context.Drivers.FirstOrDefaultAsync(d => d.Phone == dr2.Phone);
            if (driverProfile == null)
            {
                context.Drivers.Add(new Driver { 
                    DriverName = "Bưu tá driver2", 
                    Phone = dr2.Phone!, 
                    Status = "DangLam" 
                });
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine("🚀 Đã tạo thêm tài khoản Dispatcher: dispatcher / Dispatcher@123");
    }

    var finalUsers = context.Users.Select(u => new { u.Username, u.Role, u.IsApproved }).ToList();
    Console.WriteLine("📊 DANH SÁCH USER SAU KHI DỌN DẸP:");
    finalUsers.ForEach(u => Console.WriteLine($"- {u.Username} [{u.Role}] (Approved: {u.IsApproved})"));
}
app.Run();