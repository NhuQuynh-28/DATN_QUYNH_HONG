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
                a.ZoneIds,
                zoneCount   = string.IsNullOrEmpty(a.ZoneIds) ? 0 : a.ZoneIds.Split(',', System.StringSplitOptions.RemoveEmptyEntries).Length,
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
                a.ZoneIds,
                zoneCount   = string.IsNullOrEmpty(a.ZoneIds) ? 0 : a.ZoneIds.Split(',', System.StringSplitOptions.RemoveEmptyEntries).Length,
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

    // ======== ĐÁNH GIÁ CHẤT LƯỢNG PHÂN CÔNG TOÀN DIỆN ========
    [HttpGet]
    public IActionResult GetAssignmentEvaluation(int? month, int? year, int? areaId)
    {
        int m = month ?? DateTime.Now.Month;
        int y = year  ?? DateTime.Now.Year;

        // Previous month
        int pm = m == 1 ? 12 : m - 1;
        int py = m == 1 ? y - 1 : y;

        // 1. Current month assignments
        var curQuery = _context.Assignments
            .Include(a => a.Driver)
            .Include(a => a.Area)
            .Include(a => a.Dispatcher)
            .Where(a => a.Month == m && a.Year == y);

        if (areaId.HasValue && areaId > 0)
            curQuery = curQuery.Where(a => a.AreaId == areaId);

        var curAssignments = curQuery.ToList();

        // 2. Previous month assignments (same area)
        var prevQuery = _context.Assignments
            .Include(a => a.Driver)
            .Where(a => a.Month == pm && a.Year == py);

        if (areaId.HasValue && areaId > 0)
            prevQuery = prevQuery.Where(a => a.AreaId == areaId);

        var prevAssignments = prevQuery.ToList();

        // 3. All zone IDs involved
        var allZoneIds = curAssignments
            .Where(a => !string.IsNullOrEmpty(a.ZoneIds))
            .SelectMany(a => (a.ZoneIds ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse))
            .Distinct().ToList();

        // 4. Get zone info from DB
        var zones = _context.Zones
            .Where(z => allZoneIds.Contains(z.Id))
            .ToDictionary(z => z.Id);

        // 5. Get forecasts for current and prev month
        var curForecasts = _context.ZoneHistories
            .Where(h => h.Month == m && h.Year == y && allZoneIds.Contains(h.ZoneId))
            .ToList();

        var prevForecasts = _context.ZoneHistories
            .Where(h => h.Month == pm && h.Year == py)
            .ToList();

        // 6. Compute per-driver metrics
        int totalDrivers = curAssignments.Count;
        int totalPolygons = allZoneIds.Count;
        double avgPolygons = totalDrivers > 0 ? (double)totalPolygons / totalDrivers : 0;

        int totalPlannedOrders = curAssignments.Sum(a => a.PlannedOrders);
        int totalPlannedCustomers = curAssignments.Sum(a => a.PlannedCustomers);
        int totalActualOrders = curAssignments.Sum(a => a.ActualOrders);
        int totalActualCustomers = curAssignments.Sum(a => a.ActualCustomers);

        double avgOrders = totalDrivers > 0 ? (double)totalPlannedOrders / totalDrivers : 0;
        double avgCustomers = totalDrivers > 0 ? (double)totalPlannedCustomers / totalDrivers : 0;

        // Balance threshold: ±10%
        double balanceThreshold = 0.10;

        var driverDetails = curAssignments.Select(a =>
        {
            var driverZoneIds = string.IsNullOrEmpty(a.ZoneIds)
                ? new List<int>()
                : a.ZoneIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();

            int zoneCount = driverZoneIds.Count;

            // Compactness: average distance from zone centers to centroid
            double compactness = 0;
            if (driverZoneIds.Count > 1)
            {
                var zoneInfos = driverZoneIds
                    .Where(id => zones.ContainsKey(id))
                    .Select(id => zones[id])
                    .ToList();

                if (zoneInfos.Count > 0)
                {
                    double centroidLat = zoneInfos.Average(z => z.CenterLat);
                    double centroidLng = zoneInfos.Average(z => z.CenterLng);

                    compactness = zoneInfos.Sum(z =>
                        HaversineDistance(z.CenterLat, z.CenterLng, centroidLat, centroidLng));
                }
            }

            // Balance violation
            double orderDeviation = avgOrders > 0 ? Math.Abs(a.PlannedOrders - avgOrders) / avgOrders : 0;
            double customerDeviation = avgCustomers > 0 ? Math.Abs(a.PlannedCustomers - avgCustomers) / avgCustomers : 0;
            bool violatesBalance = orderDeviation > balanceThreshold || customerDeviation > balanceThreshold;
            double violationAmount = 0;
            if (violatesBalance)
            {
                if (orderDeviation > balanceThreshold)
                    violationAmount += Math.Abs(a.PlannedOrders - avgOrders) - (avgOrders * balanceThreshold);
                if (customerDeviation > balanceThreshold)
                    violationAmount += Math.Abs(a.PlannedCustomers - avgCustomers) - (avgCustomers * balanceThreshold);
            }

            // Hao hụt (difference between planned and actual)
            int orderDiff = a.PlannedOrders - a.ActualOrders;
            int customerDiff = a.PlannedCustomers - a.ActualCustomers;
            double orderDiffPct = a.PlannedOrders > 0 ? (double)orderDiff / a.PlannedOrders * 100 : 0;

            // Zone-level detail with forecast data
            var zoneDetails = driverZoneIds.Select(zid =>
            {
                var z = zones.ContainsKey(zid) ? zones[zid] : null;
                var fc = curForecasts.FirstOrDefault(f => f.ZoneId == zid);
                return new
                {
                    zoneId = zid,
                    zoneName = z?.ZoneName ?? $"Zone #{zid}",
                    ordersForecast = fc?.OrdersForecast ?? 0,
                    customersForecast = fc?.CustomersForecast ?? 0,
                    ordersReal = fc?.OrdersReal,
                    customersReal = fc?.CustomersReal
                };
            }).ToList();

            // Previous month comparison
            var prevAssignment = prevAssignments.FirstOrDefault(pa => pa.DriverId == a.DriverId);
            int prevPlannedOrders = prevAssignment?.PlannedOrders ?? 0;
            int prevActualOrders = prevAssignment?.ActualOrders ?? 0;
            int prevOrderDiff = prevPlannedOrders - prevActualOrders;
            double prevOrderDiffPct = prevPlannedOrders > 0 ? (double)prevOrderDiff / prevPlannedOrders * 100 : 0;

            return new
            {
                driverId = a.DriverId,
                driverName = a.Driver?.DriverName ?? "",
                driverPhone = a.Driver?.Phone ?? "",
                areaName = a.Area?.Name ?? "",
                dispatcher = a.Dispatcher?.Username ?? "",
                zoneCount,
                plannedOrders = a.PlannedOrders,
                plannedCustomers = a.PlannedCustomers,
                actualOrders = a.ActualOrders,
                actualCustomers = a.ActualCustomers,
                totalDistance = Math.Round(a.TotalDistance, 2),
                compactness = Math.Round(compactness, 2),
                orderDevPct = Math.Round(orderDeviation * 100, 1),
                customerDevPct = Math.Round(customerDeviation * 100, 1),
                violatesBalance,
                violationAmount = Math.Round(violationAmount, 1),
                orderDiff,
                customerDiff,
                orderDiffPct = Math.Round(orderDiffPct, 1),
                zoneIds = a.ZoneIds,
                date = a.Date.ToString("dd/MM/yyyy"),
                // Previous month comparison
                prevPlannedOrders,
                prevActualOrders,
                prevOrderDiff,
                prevOrderDiffPct = Math.Round(prevOrderDiffPct, 1),
                zones = zoneDetails
            };
        }).ToList();

        // Solution-level metrics
        var compactnessValues = driverDetails.Select(d => d.compactness).ToList();
        double compactnessMin = compactnessValues.Any() ? compactnessValues.Min() : 0;
        double compactnessMax = compactnessValues.Any() ? compactnessValues.Max() : 0;
        double compactnessAvg = compactnessValues.Any() ? compactnessValues.Average() : 0;

        int totalViolations = driverDetails.Count(d => d.violatesBalance);
        double totalViolationAmount = driverDetails.Sum(d => d.violationAmount);

        // Imbalance measure: standard deviation of orders
        double orderStdDev = 0;
        if (driverDetails.Count() > 1)
        {
            double mean = driverDetails.Average(d => d.plannedOrders);
            orderStdDev = Math.Sqrt(driverDetails.Sum(d => Math.Pow(d.plannedOrders - mean, 2)) / driverDetails.Count());
        }

        // Hao hụt totals
        int totalOrderDiff = totalPlannedOrders - totalActualOrders;
        double totalOrderDiffPct = totalPlannedOrders > 0 ? (double)totalOrderDiff / totalPlannedOrders * 100 : 0;
        int totalCustomerDiff = totalPlannedCustomers - totalActualCustomers;

        // Previous month totals for comparison
        int prevTotalPlanned = prevAssignments.Sum(a => a.PlannedOrders);
        int prevTotalActual = prevAssignments.Sum(a => a.ActualOrders);
        int prevTotalPlannedCust = prevAssignments.Sum(a => a.PlannedCustomers);
        int prevTotalActualCust = prevAssignments.Sum(a => a.ActualCustomers);
        int prevTotalDrivers = prevAssignments.Count;
        double prevTotalDiffPct = prevTotalPlanned > 0 ? (double)(prevTotalPlanned - prevTotalActual) / prevTotalPlanned * 100 : 0;

        // Change vs previous month
        double ordersChangePct = prevTotalPlanned > 0 ?
            (double)(totalPlannedOrders - prevTotalPlanned) / prevTotalPlanned * 100 : 0;
        double actualChangePct = prevTotalActual > 0 ?
            (double)(totalActualOrders - prevTotalActual) / prevTotalActual * 100 : 0;

        return Json(new
        {
            month = m,
            year = y,
            prevMonth = pm,
            prevYear = py,
            summary = new
            {
                totalDrivers,
                totalPolygons,
                avgPolygons = Math.Round(avgPolygons, 1),
                totalPlannedOrders,
                totalPlannedCustomers,
                totalActualOrders,
                totalActualCustomers,
                totalOrderDiff,
                totalCustomerDiff,
                totalOrderDiffPct = Math.Round(totalOrderDiffPct, 1),
                avgOrders = Math.Round(avgOrders, 1),
                avgCustomers = Math.Round(avgCustomers, 1),
                compactnessMin = Math.Round(compactnessMin, 2),
                compactnessMax = Math.Round(compactnessMax, 2),
                compactnessAvg = Math.Round(compactnessAvg, 2),
                totalViolations,
                totalViolationAmount = Math.Round(totalViolationAmount, 1),
                orderStdDev = Math.Round(orderStdDev, 2),
                balanceThresholdPct = balanceThreshold * 100
            },
            prevSummary = new
            {
                totalDrivers = prevTotalDrivers,
                totalPlannedOrders = prevTotalPlanned,
                totalActualOrders = prevTotalActual,
                totalPlannedCustomers = prevTotalPlannedCust,
                totalActualCustomers = prevTotalActualCust,
                totalDiffPct = Math.Round(prevTotalDiffPct, 1),
                ordersChangePct = Math.Round(ordersChangePct, 1),
                actualChangePct = Math.Round(actualChangePct, 1)
            },
            drivers = driverDetails
        });
    }

    // ======== CẬP NHẬT SỐ LIỆU THỰC TẾ ========
    [HttpPost]
    public IActionResult UpdateActual([FromBody] UpdateActualRequest req)
    {
        var assignment = _context.Assignments.Find(req.Id);
        if (assignment == null) return Json(new { success = false, message = "Không tìm thấy phân công!" });

        assignment.ActualOrders = req.ActualOrders;
        assignment.ActualCustomers = req.ActualCustomers;
        _context.SaveChanges();

        return Json(new { success = true });
    }

    public class UpdateActualRequest
    {
        public int Id { get; set; }
        public int ActualOrders { get; set; }
        public int ActualCustomers { get; set; }
    }

    private double HaversineDistance(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371000;
        double p1 = lat1 * Math.PI / 180;
        double p2 = lat2 * Math.PI / 180;
        double dp = (lat2 - lat1) * Math.PI / 180;
        double dl = (lng2 - lng1) * Math.PI / 180;
        double a = Math.Sin(dp / 2) * Math.Sin(dp / 2) +
                   Math.Cos(p1) * Math.Cos(p2) *
                   Math.Sin(dl / 2) * Math.Sin(dl / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    // Giữ các routes cũ hoạt động
    public IActionResult ByMonth()   => List();
    public IActionResult Detail()    => List();
    public IActionResult IndexDriver() => List();
}