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


    public IActionResult List()
    {
        var drivers = _context.Drivers
            .OrderByDescending(d => d.CreatedAt)
            .ToList();


        int currentMonth = DateTime.Now.Month;
        int currentYear  = DateTime.Now.Year;

        var assignmentCounts = _context.Assignments
            .Where(a => a.Date.Month == currentMonth && a.Date.Year == currentYear)
            .GroupBy(a => a.DriverId)
            .Select(g => new { DriverId = g.Key, Count = g.Count() })
            .ToList();

        ViewBag.AssignmentCounts = assignmentCounts
            .ToDictionary(x => x.DriverId, x => x.Count);

        ViewBag.Areas = _context.Areas
            .Select(a => new { a.Id, a.Name })
            .ToList();

        ViewBag.TotalDrivers   = drivers.Count;
        ViewBag.ActiveDrivers  = drivers.Count(d => d.Status == "DangLam");
        ViewBag.InactiveDrivers = drivers.Count(d => d.Status != "DangLam");

        return PartialView(drivers);
    }

 
    [HttpPost]
    public IActionResult Create([FromBody] Driver d)
    {
        if (d == null || string.IsNullOrWhiteSpace(d.DriverName))
            return Json(new { success = false, message = "Thiếu thông tin tài xế!" });

        d.CreatedAt = DateTime.Now;
        if (string.IsNullOrWhiteSpace(d.Status)) d.Status = "DangLam";

        _context.Drivers.Add(d);
        _context.SaveChanges();
        return Json(new { success = true, id = d.Id });
    }

    [HttpGet]
    public IActionResult GetById(int id)
    {
        var d = _context.Drivers.Find(id);
        if (d == null) return Json(new { success = false });
        return Json(new { success = true, id = d.Id, driverName = d.DriverName, phone = d.Phone, status = d.Status });
    }

    [HttpPost]
    public IActionResult Update([FromBody] Driver d)
    {
        var existing = _context.Drivers.Find(d.Id);
        if (existing == null) return Json(new { success = false, message = "Không tìm thấy tài xế!" });

        existing.DriverName = d.DriverName;
        existing.Phone      = d.Phone;
        existing.Status     = d.Status;
        _context.SaveChanges();
        return Json(new { success = true });
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var d = _context.Drivers.Find(id);
        if (d == null) return Json(new { success = false, message = "Không tìm thấy!" });

        // Check xem có assignment không
        bool hasAssignment = _context.Assignments.Any(a => a.DriverId == id);
        if (hasAssignment)
            return Json(new { success = false, message = "Tài xế đang có lịch phân công, không thể xóa!" });

        _context.Drivers.Remove(d);
        _context.SaveChanges();
        return Json(new { success = true });
    }

    // ======== XEM THEO THÁNG (trả JSON) ========
    [HttpGet]
    public IActionResult GetByMonth(int? month, int? year, int? areaId, string driverName)
    {
        int m = month ?? DateTime.Now.Month;
        int y = year  ?? DateTime.Now.Year;

        var query = _context.Assignments
            .Include(a => a.Driver)
            .Include(a => a.Area)
            .Include(a => a.Dispatcher)
            .Where(a => a.Date.Month == m && a.Date.Year == y)
            .AsQueryable();

        if (areaId.HasValue && areaId > 0)
            query = query.Where(x => x.AreaId == areaId);

        if (!string.IsNullOrWhiteSpace(driverName))
            query = query.Where(x => x.Driver.DriverName.Contains(driverName));

        var data = query
            .OrderByDescending(x => x.Date)
            .Select(a => new {
                a.Id,
                driverName  = a.Driver != null ? a.Driver.DriverName : "",
                driverPhone = a.Driver != null ? a.Driver.Phone : "",
                areaName    = a.Area   != null ? a.Area.Name   : "",
                dispatcher  = a.Dispatcher != null ? a.Dispatcher.Username : "",
                a.PlannedOrders,
                a.ActualOrders,
                date        = a.Date.ToString("dd/MM/yyyy"),
                isOk        = a.ActualOrders >= a.PlannedOrders
            })
            .ToList();

        return Json(data);
    }

    // ======== THỐNG KÊ TÓM TẮT THÁNG ========
    [HttpGet]
    public IActionResult GetMonthlySummary(int? month, int? year)
    {
        int m = month ?? DateTime.Now.Month;
        int y = year  ?? DateTime.Now.Year;

        var assignments = _context.Assignments
            .Where(a => a.Date.Month == m && a.Date.Year == y)
            .ToList();

        return Json(new {
            totalAssignments = assignments.Count,
            totalPlanned     = assignments.Sum(a => a.PlannedOrders),
            totalActual      = assignments.Sum(a => a.ActualOrders),
            completionRate   = assignments.Count > 0
                ? Math.Round((double)assignments.Count(a => a.ActualOrders >= a.PlannedOrders) / assignments.Count * 100, 1)
                : 0
        });
    }

    // ======== TẤT CẢ PHÂN CÔNG (tab Detail) ========
    [HttpGet]
    public IActionResult GetAllAssignments(string driverName, int? areaId, string status)
    {
        var query = _context.Assignments
            .Include(a => a.Driver)
            .Include(a => a.Area)
            .Include(a => a.Dispatcher)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(driverName))
            query = query.Where(x => x.Driver.DriverName.Contains(driverName));

        if (areaId.HasValue && areaId > 0)
            query = query.Where(x => x.AreaId == areaId);

        if (status == "ok")
            query = query.Where(x => x.ActualOrders >= x.PlannedOrders);
        else if (status == "fail")
            query = query.Where(x => x.ActualOrders < x.PlannedOrders);

        var data = query
            .OrderByDescending(x => x.Date)
            .Select(a => new {
                a.Id,
                driverName  = a.Driver     != null ? a.Driver.DriverName    : "",
                driverPhone = a.Driver     != null ? a.Driver.Phone          : "",
                areaName    = a.Area       != null ? a.Area.Name             : "",
                dispatcher  = a.Dispatcher != null ? a.Dispatcher.Username   : "",
                a.PlannedOrders,
                a.ActualOrders,
                date        = a.Date.ToString("dd/MM/yyyy"),
                month       = a.Date.Month,
                year        = a.Date.Year,
                isOk        = a.ActualOrders >= a.PlannedOrders
            })
            .ToList();

        return Json(data);
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var areas = _context.Areas
            .Select(a => new { id = a.Id, areaName = a.Name })
            .ToList();
        return Json(areas);
    }

    // Giữ các routes cũ hoạt động
    public IActionResult ByMonth()   => List();
    public IActionResult Detail()    => List();
    public IActionResult IndexDriver() => List();
}