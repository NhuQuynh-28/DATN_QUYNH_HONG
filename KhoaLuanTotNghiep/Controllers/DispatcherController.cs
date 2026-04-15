using Microsoft.AspNetCore.Mvc;
using KhoaLuanTotNghiep.Data;
using KhoaLuanTotNghiep.Models;
using KhoaLuanTotNghiep.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace KhoaLuanTotNghiep.Controllers
{
    public class DispatcherController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ForecastingService _forecasting;
        private readonly HeuristicAssignmentService _heuristic;

        public DispatcherController(
            AppDbContext context,
            ForecastingService forecasting,
            HeuristicAssignmentService heuristic)
        {
            _context    = context;
            _forecasting = forecasting;
            _heuristic   = heuristic;
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None, Duration = 0)]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Lấy tất cả khu vực + version active + zones
        /// </summary>
        [HttpGet]
        public IActionResult GetAllAreas()
        {
            var areas = _context.Areas
                .Select(a => new
                {
                    id = a.Id,
                    name = a.Name,
                    status = a.Status
                })
                .ToList();

            return Json(areas);
        }

        /// <summary>
        /// Lấy danh sách zone theo area (qua version active)
        /// </summary>
        [HttpGet]
        public IActionResult GetZonesByArea(int areaId)
        {
            var activeVersion = _context.ZoneVersions
                .FirstOrDefault(v => v.AreaId == areaId && v.IsActive == true);

            if (activeVersion == null)
                return Json(new { zones = new object[0], versionId = 0, versionName = "" });

            var zones = _context.Zones
                .Where(z => z.VersionId == activeVersion.Id)
                .Select(z => new
                {
                    z.Id,
                    z.ZoneName,
                    z.Points,
                    z.CenterLat,
                    z.CenterLng,
                    z.Area,
                    z.Order,
                    z.Customers
                })
                .ToList();

            return Json(new
            {
                zones,
                versionId = activeVersion.Id,
                versionName = activeVersion.VersionName
            });
        }

        /// <summary>
        /// Lấy trạng thái forecast cho tất cả zone theo tháng/năm
        /// </summary>
        [HttpGet]
        public IActionResult GetForecastStatus(int month, int year, int? areaId, string filter)
        {
            // Lấy tất cả version active, include Area để lấy Name
            var activeVersions = _context.ZoneVersions
                .Include(v => v.Area)
                .Where(v => v.IsActive == true);

            if (areaId.HasValue && areaId > 0)
                activeVersions = activeVersions.Where(v => v.AreaId == areaId);

            var activeVersionList = activeVersions.ToList();
            var versionIds = activeVersionList.Select(v => v.Id).ToList();

            var zones = _context.Zones
                .Where(z => z.VersionId != null && versionIds.Contains(z.VersionId.Value))
                .ToList();

            // Lấy forecast cho tháng/năm này
            var forecasts = _context.ZoneHistories
                .Where(h => h.Month == month && h.Year == year)
                .ToList();

            var result = zones.Select(z =>
            {
                var fc = forecasts.FirstOrDefault(f => f.ZoneId == z.Id);
                bool hasFC = fc != null && (fc.OrdersForecast > 0 || fc.CustomersForecast > 0);

                // Lấy thông tin area từ version đã load sẵn trong list
                var version = activeVersionList.FirstOrDefault(v => v.Id == z.VersionId);
                var areaName = version?.Area?.Name ?? z.AreaName ?? "—";
                var areaIdVal = version?.AreaId ?? 0;

                return new
                {
                    zoneId = z.Id,
                    zoneName = z.ZoneName ?? $"Vùng #{z.Id}", // Tránh NULL
                    areaName = areaName,
                    areaId = areaIdVal,
                    centerLat = z.CenterLat,
                    centerLng = z.CenterLng,
                    points = z.Points,
                    zoneArea = z.Area,
                    hasForecast = hasFC,
                    ordersForecast = fc?.OrdersForecast ?? 0,
                    customersForecast = fc?.CustomersForecast ?? 0,
                    ordersReal = fc?.OrdersReal,
                    customersReal = fc?.CustomersReal
                };
            }).ToList();

            // Áp dụng filter
            if (filter == "missing")
                result = result.Where(x => !x.hasForecast).ToList();
            else if (filter == "entered")
                result = result.Where(x => x.hasForecast).ToList();

            int total = zones.Count;
            int entered = result.Count(x => x.hasForecast);
            int missing = result.Count(x => !x.hasForecast);
            int totalOrders = result.Sum(x => x.ordersForecast);
            int totalCustomers = result.Sum(x => x.customersForecast);

            return Json(new
            {
                zones = result,
                summary = new
                {
                    total,
                    entered,
                    missing,
                    totalOrders,
                    totalCustomers
                }
            });
        }

        /// <summary>
        /// Lấy lịch sử zone (cho chart)
        /// </summary>
        [HttpGet]
        public IActionResult GetZoneHistory(int zoneId)
        {
            int curMonth = DateTime.Now.Month;
            int curYear  = DateTime.Now.Year;

            // Next month boundary
            int nextMonth = curMonth == 12 ? 1  : curMonth + 1;
            int nextYear  = curMonth == 12 ? curYear + 1 : curYear;

            var raw = _context.ZoneHistories
                .Where(z => z.ZoneId == zoneId && z.Month > 0 && z.Year > 0)
                .OrderBy(z => z.Year)
                .ThenBy(z => z.Month)
                .Select(z => new
                {
                    month             = z.Month,
                    year              = z.Year,
                    ordersReal        = z.OrdersReal,
                    customersReal     = z.CustomersReal,
                    ordersForecast    = z.OrdersForecast,
                    customersForecast = z.CustomersForecast
                })
                .ToList();

            // Keep row if:
            //  a) has real data (ordersReal or customersReal is not null), OR
            //  b) is before or equal to next month (current + 1) — for planning visibility
            var data = raw.Where(x =>
                x.ordersReal != null || x.customersReal != null ||
                (x.year < nextYear) ||
                (x.year == nextYear && x.month <= nextMonth)
            ).ToList();

            return Json(data);
        }

        /// <summary>
        /// Dự đoán đơn giản (moving average 3 tháng)
        /// </summary>
        [HttpGet]
        public IActionResult PredictForecast(int zoneId)
        {
            var data = _context.ZoneHistories
                .Where(x => x.ZoneId == zoneId && x.OrdersReal != null)
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .Take(3)
                .ToList();

            if (data.Count < 3)
            {
                // Nếu thiếu data, lấy forecast gần nhất
                var lastFc = _context.ZoneHistories
                    .Where(x => x.ZoneId == zoneId && x.OrdersForecast > 0)
                    .OrderByDescending(x => x.Year)
                    .ThenByDescending(x => x.Month)
                    .FirstOrDefault();

                return Json(new
                {
                    order = lastFc?.OrdersForecast ?? 0,
                    customer = lastFc?.CustomersForecast ?? 0,
                    method = "last_known"
                });
            }

            double[] weights = { 0.5, 0.3, 0.2 };
            double orderPredict = 0;
            double customerPredict = 0;

            for (int i = 0; i < data.Count; i++)
            {
                orderPredict += data[i].OrdersReal.Value * weights[i];
                customerPredict += data[i].CustomersReal.Value * weights[i];
            }

            return Json(new
            {
                order = (int)Math.Round(orderPredict),
                customer = (int)Math.Round(customerPredict),
                method = "weighted_avg"
            });
        }

        /// <summary>
        /// Lưu forecast cho 1 zone
        /// </summary>
        [HttpPost]
        public IActionResult SaveForecast([FromBody] ForecastInput input)
        {
            var item = _context.ZoneHistories
                .FirstOrDefault(x => x.ZoneId == input.ZoneId
                                  && x.Month == input.Month
                                  && x.Year == input.Year);

            if (item == null)
            {
                item = new ZoneHistory
                {
                    ZoneId = input.ZoneId,
                    Month = input.Month,
                    Year = input.Year
                };
                _context.ZoneHistories.Add(item);
            }

            item.OrdersForecast = input.OrdersForecast;
            item.CustomersForecast = input.CustomersForecast;

            _context.SaveChanges();

            return Json(new { success = true, zoneName = _context.Zones.Find(input.ZoneId)?.ZoneName });
        }

        /// <summary>
        /// Lưu forecast hàng loạt
        /// </summary>
        [HttpPost]
        public IActionResult SaveBatchForecast([FromBody] BatchForecastInput input)
        {
            int saved = 0;
            foreach (var item in input.Items)
            {
                if (item.OrdersForecast <= 0 && item.CustomersForecast <= 0) continue;

                var existing = _context.ZoneHistories
                    .FirstOrDefault(x => x.ZoneId == item.ZoneId
                                      && x.Month == input.Month
                                      && x.Year == input.Year);

                if (existing == null)
                {
                    existing = new ZoneHistory
                    {
                        ZoneId = item.ZoneId,
                        Month = input.Month,
                        Year = input.Year
                    };
                    _context.ZoneHistories.Add(existing);
                }

                existing.OrdersForecast = item.OrdersForecast;
                existing.CustomersForecast = item.CustomersForecast;
                saved++;
            }

            _context.SaveChanges();
            return Json(new { success = true, count = saved });
        }

        // ══════════════════════════════════════════════════════
        // ARIMA Forecast Endpoint
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Dự báo số đơn / khách cho tháng tới bằng ARIMA
        /// GET /Dispatcher/ForecastArima?zoneId=1
        /// </summary>
        [HttpGet]
        public IActionResult ForecastArima(int zoneId)
        {
            try
            {
                var result = _forecasting.ForecastZone(zoneId);
                return Json(new
                {
                    order       = result.Order,
                    orderLow    = result.OrderLow,
                    orderHigh   = result.OrderHigh,
                    customer    = result.Customer,
                    customerLow = result.CustomerLow,
                    customerHigh= result.CustomerHigh,
                    method      = result.Method,
                    dataPoints  = result.DataPoints
                });
            }
            catch (Exception ex)
            {
                return Json(new { order = 0, customer = 0, method = "error", message = ex.Message });
            }
        }

        // ══════════════════════════════════════════════════════
        // Get Drivers
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// GET /Dispatcher/GetDrivers — lấy danh sách tài xế đang hoạt động
        /// </summary>
        [HttpGet]
        public IActionResult GetDrivers()
        {
            var drivers = _context.Drivers
                .Where(d => d.Status == "DangLam")
                .Select(d => new { id = d.Id, name = d.DriverName, phone = d.Phone, status = d.Status })
                .ToList();
            return Json(drivers);
        }

        /// <summary>
        /// GET /Dispatcher/GetDriversByArea — lấy tài xế theo khu vực
        /// Kết hợp bảng Drivers (status) và Users (AreaId)
        /// </summary>
        [HttpGet]
        public IActionResult GetDriversByArea(int? areaId)
        {
            // Lấy tất cả tài xế đang làm
            var driversQuery = _context.Drivers.Where(d => d.Status == "DangLam");
            var drivers = driversQuery.ToList();

            // Lấy users có Role=Driver để map AreaId
            var driverUsers = _context.Users
                .Where(u => u.Role == "Driver" && u.IsActive)
                .ToList();

            // Lấy assignments hiện tại để biết tài xế đang gán ở đâu
            int curMonth = DateTime.Now.Month;
            int curYear = DateTime.Now.Year;
            var currentAssignments = _context.Assignments
                .Include(a => a.Area)
                .Where(a => a.Month == curMonth && a.Year == curYear)
                .ToList();

            var result = drivers.Select(d =>
            {
                // Tìm user tương ứng (match theo phone hoặc tên)
                var matchingUser = driverUsers.FirstOrDefault(u =>
                    (!string.IsNullOrEmpty(u.Phone) && u.Phone == d.Phone) ||
                    (!string.IsNullOrEmpty(u.Username) && u.Username == d.DriverName));

                int? driverAreaId = matchingUser?.AreaId;
                string areaName = "";
                if (driverAreaId.HasValue)
                {
                    areaName = _context.Areas.Find(driverAreaId.Value)?.Name ?? "";
                }

                // Assignment info cho tháng hiện tại
                var assignment = currentAssignments.FirstOrDefault(a => a.DriverId == d.Id);

                return new
                {
                    id = d.Id,
                    name = d.DriverName,
                    phone = d.Phone,
                    status = d.Status,
                    areaId = driverAreaId,
                    areaName,
                    hasAssignment = assignment != null,
                    assignedAreaName = assignment?.Area?.Name ?? "",
                    plannedOrders = assignment?.PlannedOrders ?? 0,
                    zoneCount = !string.IsNullOrEmpty(assignment?.ZoneIds)
                        ? assignment.ZoneIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Length : 0
                };
            }).ToList();

            // Filter by area nếu có
            if (areaId.HasValue && areaId > 0)
            {
                // Lọc tài xế thuộc khu vực hoặc chưa gán khu vực
                result = result.Where(d => d.areaId == areaId || d.areaId == null).ToList();
            }

            return Json(result);
        }

        // ══════════════════════════════════════════════════════
        // Auto-Assign: Greedy + Local Search + ALNS
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// POST /Dispatcher/AutoAssign
        /// Body: { areaId, month, year, maxOrdersPerDriver, maxCustomersPerDriver }
        /// </summary>
        [HttpPost]
        public IActionResult AutoAssign([FromBody] AutoAssignRequest req)
        {
            try
            {
                // ── 1. Load active zones for the area
                var activeVer = _context.ZoneVersions
                    .FirstOrDefault(v => v.AreaId == req.AreaId && v.IsActive == true);
                if (activeVer == null)
                    return Json(new { success = false, message = "Không có phiên bản vùng đang kích hoạt." });

                var zones = _context.Zones
                    .Where(z => z.VersionId == activeVer.Id)
                    .ToList();

                if (zones.Count == 0)
                    return Json(new { success = false, message = "Không có vùng nào trong khu vực này." });

                // ── 2. Get ARIMA forecast for each zone
                var zoneIds    = zones.Select(z => z.Id).ToList();
                var forecasts  = _forecasting.ForecastBatch(zoneIds);

                var zoneNodes = zones.Select(z =>
                {
                    var fc = forecasts.ContainsKey(z.Id) ? forecasts[z.Id] : null;
                    return new ZoneNode
                    {
                        ZoneId            = z.Id,
                        ZoneName          = z.ZoneName ?? $"Zone {z.Id}",
                        CenterLat         = z.CenterLat,
                        CenterLng         = z.CenterLng,
                        ForecastOrders    = fc?.Order    ?? z.Order,
                        ForecastCustomers = fc?.Customer ?? z.Customers
                    };
                }).ToList();

                // ── 3. Load drivers (hỗ trợ chọn cụ thể hoặc lấy top N)
                List<DriverSlot> drivers;
                if (req.DriverIds != null && req.DriverIds.Any())
                {
                    drivers = _context.Drivers
                        .Where(d => req.DriverIds.Contains(d.Id) && d.Status == "DangLam")
                        .Select(d => new DriverSlot { DriverId = d.Id, DriverName = d.DriverName })
                        .ToList();
                }
                else
                {
                    drivers = _context.Drivers
                        .Where(d => d.Status == "DangLam")
                        .Take(req.DriverCount > 0 ? req.DriverCount : 10)
                        .Select(d => new DriverSlot { DriverId = d.Id, DriverName = d.DriverName })
                        .ToList();
                }

                if (drivers.Count == 0)
                    return Json(new { success = false, message = "Không có tài xế đang làm việc." });

                // ── 4. Run heuristic pipeline
                var result = _heuristic.RunFullPipeline(
                    drivers, zoneNodes,
                    req.MaxOrdersPerDriver,
                    req.MaxCustomersPerDriver);

                // ── 5. Serialize result
                return Json(new
                {
                    success         = true,
                    improvementPct  = result.ImprovementPct,
                    elapsedMs       = result.ElapsedMs,
                    objectiveHistory= result.ObjectiveHistory,
                    greedyObjective = Math.Round(result.GreedyInit.Objective, 2),
                    bestObjective   = Math.Round(result.Best.Objective, 2),
                    bestPhase       = result.Best.Phase,
                    bestIteration   = result.Best.Iteration,
                    slots           = result.Best.Slots.Select(s => new
                    {
                        driverId       = s.DriverId,
                        driverName     = s.DriverName,
                        totalOrders    = s.TotalOrders,
                        totalCustomers = s.TotalCustomers,
                        totalDistance  = s.TotalDistance, // <--- Sửa lỗi tham chiếu
                        zoneCount      = s.Zones.Count,
                        zones          = s.Zones.Select(z => new
                        {
                            zoneId     = z.ZoneId,
                            zoneName   = z.ZoneName,
                            orders     = z.ForecastOrders,
                            customers  = z.ForecastCustomers
                        })
                    }),
                    metrics = new
                    {
                        penaltyOrders    = Math.Round(result.Best.PenaltyOrders,    2),
                        penaltyCustomers = Math.Round(result.Best.PenaltyCustomers, 2),
                        compactness      = Math.Round(result.Best.Compactness,       4)
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST /Dispatcher/SaveAssignments
        /// Lưu kết quả phân công ALNS vào database
        /// </summary>
        [HttpPost]
        public IActionResult SaveAssignments([FromBody] SaveAssignmentRequest req)
        {
            try
            {
                if (req.Items == null || !req.Items.Any())
                    return Json(new { success = false, message = "Không có dữ liệu để lưu." });

                // ── 1. Clear old assignments for this (Area, Month, Year)
                var old = _context.Assignments
                    .Where(a => a.AreaId == req.AreaId && a.Month == req.Month && a.Year == req.Year)
                    .ToList();
                
                if (old.Any()) _context.Assignments.RemoveRange(old);

                // ── 2. Get Current Dispatcher Id
                var username = HttpContext.Session.GetString("Username");
                var currentUser = _context.Users.FirstOrDefault(u => u.Username == username);
                if (currentUser == null)
                    return Json(new { success = false, message = "Không tìm thấy thông tin người điều phối (vui lòng đăng nhập lại)." });

                // ── 3. Create new assignments
                var now = DateTime.Now;
                foreach (var item in req.Items)
                {
                    var assignment = new Assignment
                    {
                        DriverId         = item.DriverId,
                        DispatcherId     = currentUser.Id, // <--- Cần thiết
                        AreaId           = req.AreaId,
                        Month            = req.Month,
                        Year             = req.Year,
                        Date             = now,
                        PlannedOrders    = item.TotalOrders,
                        PlannedCustomers = item.TotalCustomers,
                        TotalDistance    = item.TotalDistance,
                        ZoneIds          = string.Join(",", item.ZoneIds),
                        ActualOrders     = 0,
                        ActualCustomers  = 0
                    };
                    _context.Assignments.Add(assignment);
                }

                _context.SaveChanges();
                return Json(new { success = true, message = $"Đã lưu phân công cho {req.Items.Count} tài xế thành công!" });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Lỗi khi lưu: " + msg });
            }
        }

        public class SaveAssignmentRequest
        {
            public int AreaId { get; set; }
            public int Month { get; set; }
            public int Year { get; set; }
            public List<SaveAssignmentItem> Items { get; set; }
        }

        public class SaveAssignmentItem
        {
            public int DriverId { get; set; }
            public int TotalOrders { get; set; }
            public int TotalCustomers { get; set; }
            public double TotalDistance { get; set; }
            public List<int> ZoneIds { get; set; }
        }

        public class AutoAssignRequest
        {
            public int AreaId               { get; set; }
            public int Month                { get; set; }
            public int Year                 { get; set; }
            public int DriverCount          { get; set; } = 10;
            public List<int> DriverIds      { get; set; }  // Danh sách ID tài xế cụ thể
            public int MaxOrdersPerDriver   { get; set; } = int.MaxValue;
            public int MaxCustomersPerDriver{ get; set; } = int.MaxValue;
        }

        // DTO classes
        public class ForecastInput
        {
            public int ZoneId { get; set; }
            public int Month { get; set; }
            public int Year { get; set; }
            public int OrdersForecast { get; set; }
            public int CustomersForecast { get; set; }
        }

        public class BatchForecastInput
        {
            public int Month { get; set; }
            public int Year { get; set; }
            public List<BatchForecastItem> Items { get; set; }
        }

        public class BatchForecastItem
        {
            public int ZoneId { get; set; }
            public int OrdersForecast { get; set; }
            public int CustomersForecast { get; set; }
        }
    }
}
