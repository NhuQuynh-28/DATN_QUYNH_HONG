using Microsoft.AspNetCore.Mvc;
using KhoaLuanTotNghiep.Data;
using KhoaLuanTotNghiep.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace KhoaLuanTotNghiep.Controllers
{
    public class DriverDashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DriverDashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None, Duration = 0)]
        public IActionResult Index(int? month, int? year)
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role != "Driver")
            {
                return RedirectToAction("Login", "Account");
            }

            // 1. Tìm DriverId từ số điện thoại của User
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return NotFound("User not found");

            var driver = _context.Drivers.FirstOrDefault(d => d.Phone == user.Phone);
            if (driver == null)
            {
                ViewBag.NoDriverRecord = true;
                return View();
            }

            // 2. Lấy danh sách tất cả tháng/năm có phân công của tài xế này
            var allMonthData = _context.Assignments
                .Where(a => a.DriverId == driver.Id)
                .Select(a => new { a.Month, a.Year })
                .Distinct()
                .OrderByDescending(a => a.Year)
                .ThenByDescending(a => a.Month)
                .ToList();

            // Chuyển thành List<string> "month-year" để dễ dùng trong View
            var availableMonthKeys = allMonthData.Select(a => $"{a.Month}-{a.Year}").ToList();
            ViewBag.AvailableMonthKeys = availableMonthKeys;
            // Để hiển thị label, dùng cùng thứ tự
            ViewBag.AvailableMonths = allMonthData.Select(a => new int[] { a.Month, a.Year }).ToList();

            if (!allMonthData.Any())
            {
                ViewBag.Driver = driver;
                ViewBag.NoAssignment = true;
                return View();
            }

            // 3. Xác định tháng/năm cần hiển thị
            var now = DateTime.Now;
            int selectedMonth = month ?? now.Month;
            int selectedYear  = year  ?? now.Year;

            // Nếu tháng/năm được chọn không có dữ liệu, lấy mới nhất
            bool exists = allMonthData.Any(a => a.Month == selectedMonth && a.Year == selectedYear);
            if (!exists)
            {
                selectedMonth = allMonthData.First().Month;
                selectedYear  = allMonthData.First().Year;
            }

            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;

            // 4. Lấy phân công theo tháng/năm đã chọn
            var assignment = _context.Assignments
                .Include(a => a.Area)
                .FirstOrDefault(a => a.DriverId == driver.Id
                                  && a.Month == selectedMonth
                                  && a.Year == selectedYear);

            if (assignment == null)
            {
                ViewBag.Driver = driver;
                ViewBag.NoAssignment = true;
                return View();
            }

            // 5. Lấy thông tin các vùng chi tiết
            var zoneIds = string.IsNullOrEmpty(assignment.ZoneIds)
                ? new List<int>()
                : assignment.ZoneIds.Split(',').Select(int.Parse).ToList();

            var zones = _context.Zones
                .Where(z => zoneIds.Contains(z.Id))
                .ToList();

            ViewBag.Driver = driver;
            ViewBag.Assignment = assignment;
            ViewBag.Zones = zones;

            return View();
        }
    }
}
