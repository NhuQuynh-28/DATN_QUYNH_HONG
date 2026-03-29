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
        public IActionResult Index()
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
                // Nếu chưa có Driver record tương ứng, trả về View rỗng kèm thông báo
                ViewBag.NoDriverRecord = true;
                return View();
            }

            // 2. Lấy phân công mới nhất (tháng hiện tại)
            var now = DateTime.Now;
            var assignment = _context.Assignments
                .Include(a => a.Area)
                .Where(a => a.DriverId == driver.Id)
                .OrderByDescending(a => a.Year)
                .ThenByDescending(a => a.Month)
                .FirstOrDefault();

            if (assignment == null)
            {
                ViewBag.NoAssignment = true;
                return View();
            }

            // 3. Lấy thông tin các vùng chi tiết
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
