using Microsoft.AspNetCore.Mvc;
using KhoaLuanTotNghiep.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace KhoaLuanTotNghiep.Controllers
{
    public class ReportController : Controller
    {
        private readonly AppDbContext _context;

        public ReportController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // 1. Dashboard chính
        public IActionResult Dashboard()
        {
            return PartialView("_Dashboard");
        }

        [HttpGet]
        public IActionResult DashboardData()
        {
            var stats = new
            {
                totalArea = _context.Areas.Count(),
                totalVersion = _context.ZoneVersions.Count(),
                totalZone = _context.Zones.Count(),
                totalShipper = _context.Drivers.Count(),
                totalUsers = _context.Users.Count(),
                totalOrders = _context.ZoneHistories.Any() ? _context.ZoneHistories.Sum(zh => zh.OrdersReal ?? 0) : 0,
                totalCustomers = _context.ZoneHistories.Any() ? _context.ZoneHistories.Sum(zh => zh.CustomersReal ?? 0) : 0,
                activeDrivers = _context.Drivers.Count(d => d.Status == "DangLam"),
                pendingUsers = _context.Users.Count(u => u.IsApproved != true)
            };
            return Json(stats);
        }

        // 2. Thống kê theo Vùng
        public IActionResult ZoneReport()
        {
            return PartialView("_ZoneReport");
        }

        [HttpGet]
        public IActionResult ZoneReportData()
        {
            var zonesList = _context.Zones
                .Select(z => new {
                    z.Id,
                    z.ZoneName,
                    AreaName = z.AreaName ?? "N/A"
                })
                .ToList();

            var zoneIds = zonesList.Select(z => z.Id).ToList();
            var histories = _context.ZoneHistories
                .Where(zh => zoneIds.Contains(zh.Id))
                .ToList();

            var zones = zonesList
                .Select(z => new {
                    z.Id,
                    z.ZoneName,
                    z.AreaName,
                    TotalOrders = _context.ZoneHistories.Where(zh => zh.ZoneId == z.Id).Sum(zh => zh.OrdersReal ?? 0),
                    TotalCustomers = _context.ZoneHistories.Where(zh => zh.ZoneId == z.Id).Sum(zh => zh.CustomersReal ?? 0)
                })
                .OrderByDescending(z => z.TotalOrders)
                .Take(10)
                .ToList();
            return Json(zones);
        }

        // 3. Thống kê theo Bưu tá
        public IActionResult DriverReport()
        {
            return PartialView("_DriverReport");
        }

        [HttpGet]
        public IActionResult DriverReportData()
        {
            // Tải dữ liệu cơ bản về trước để tính toán client-side (tránh lỗi dịch LINQ to SQL với các hàm xử lý chuỗi phức tạp)
            var driverData = _context.Drivers
                .Select(d => new {
                    d.Id,
                    d.DriverName,
                    d.Phone,
                    d.Status,
                    Assignments = _context.Assignments
                        .Where(a => a.DriverId == d.Id)
                        .Select(a => new { a.ActualOrders, a.ZoneIds })
                        .ToList()
                })
                .ToList();

            var result = driverData.Select(d => new {
                d.Id,
                d.DriverName,
                d.Phone,
                d.Status,
                ZonesCount = d.Assignments.Sum(a => string.IsNullOrEmpty(a.ZoneIds) ? 0 : a.ZoneIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Length),
                OrdersHandled = d.Assignments.Sum(a => a.ActualOrders)
            })
            .OrderByDescending(d => d.OrdersHandled)
            .ToList();

            return Json(result);
        }

        // 4. Thống kê Đơn hàng theo thời gian
        public IActionResult OrderReport()
        {
            return PartialView("_OrderReport");
        }

        [HttpGet]
        public IActionResult OrderReportData()
        {
            // Bước 1: Lấy dữ liệu thô về bộ nhớ để tránh lỗi dịch (translation error)
            var rawData = _context.ZoneHistories
                .Select(zh => new { 
                    zh.Year, 
                    zh.Month, 
                    OrdersReal = zh.OrdersReal ?? 0, 
                    CustomersReal = zh.CustomersReal ?? 0,
                    zh.OrdersForecast 
                })
                .ToList();

            // Bước 2: Nhóm và tính toán trên bộ nhớ (Client-side)
            var history = rawData
                .GroupBy(zh => new { zh.Year, zh.Month })
                .Select(g => new {
                    Label = $"T{g.Key.Month}/{g.Key.Year}",
                    Orders = g.Sum(zh => zh.OrdersReal),
                    Customers = g.Sum(zh => zh.CustomersReal),
                    Forecast = g.Sum(zh => zh.OrdersForecast)
                })
                .OrderBy(g => g.Label)
                .ToList();

            return Json(history);
        }
    }
}
