using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhoaLuanTotNghiep.Models;
using System.Linq;
using KhoaLuanTotNghiep.Data;

public class DriverController : Controller
{
    private readonly AppDbContext _context;

    public DriverController(AppDbContext context)
    {
        _context = context;
    }

    // 📅 Xem theo tháng
    // 👉 LOAD TRANG CHÍNH (dropdown)
    public IActionResult ByMonth()
    {
        return PartialView(); // trả về ByMonth.cshtml
    }

    // 👉 LOAD DATA
    public IActionResult GetByMonth(int? month, int? areaId, string driverName)
    {
        var query = _context.Assignments
            .Include(a => a.Driver)
            .Include(a => a.Area)
            .Include(a => a.Dispatcher)
            .AsQueryable();

        if (month.HasValue && month > 0)
        {
            query = query.Where(x => x.Date.Month == month);
        }

        if (areaId.HasValue && areaId > 0)
        {
            query = query.Where(x => x.AreaId == areaId);
        }

        if (!string.IsNullOrEmpty(driverName))
        {
            query = query.Where(x => x.Driver.DriverName.Contains(driverName));
        }

        var data = query.ToList();

        return PartialView("_ByMonthTable", data);
    }
    // 📋 Danh sách tài xế
    public IActionResult List()
    {
        var drivers = _context.Drivers.ToList();
        return PartialView(drivers);
    }

    // 📊 GỘP TẤT CẢ
    public IActionResult Detail()
    {
        var data = _context.Assignments
            .Include(a => a.Driver)
            .Include(a => a.Dispatcher)
            .Include(a => a.Area)
            .OrderByDescending(x => x.Date)
            .ToList();

        return PartialView(data);
    }
    public IActionResult GetAll()
    {
        var areas = _context.Areas
            .Select(a => new {
                id = a.Id,
                areaName = a.Name
            })
            .ToList();

        return Json(areas);
    }
    [HttpPost]
    public IActionResult Create([FromBody] Driver d)
    {
        if (d == null)
            return Json(new { success = false });

        _context.Drivers.Add(d);
        _context.SaveChanges();

        return Json(new { success = true });
    }
}