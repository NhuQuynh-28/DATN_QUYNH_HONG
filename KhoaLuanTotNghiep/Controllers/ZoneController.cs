using Microsoft.AspNetCore.Mvc;
using KhoaLuanTotNghiep.Data;
using KhoaLuanTotNghiep.Models;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace KhoaLuanTotNghiep.Controllers
{
    public class ZoneController : Controller
    {
        private readonly AppDbContext _context;

        public ZoneController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Map(int versionId)
        {
            var version = _context.ZoneVersions
                .Include(v => v.Area)
                .FirstOrDefault(v => v.Id == versionId);

            if (version != null)
            {
                ViewBag.VersionName = version.VersionName;
                ViewBag.AreaName = version.Area?.Name;
            }

            ViewBag.VersionId = versionId;
            return PartialView("~/Views/Zone/_ZoneMap.cshtml");
        }

        [HttpGet]
        public IActionResult GetZones(int versionId)
        {
            var zones = _context.Zones
                .Where(z => z.VersionId == versionId)
                .Select(z => new
                {
                    z.Id,
                    z.ZoneName,
                    z.Points,
                    z.CenterLat,
                    z.CenterLng,
                    z.Area
                })
                .ToList();

            return Json(zones);
        }
        private void UpdateVersionStats(int versionId)
        {
            var zones = _context.Zones
                .Where(z => z.VersionId == versionId).ToList();

            var version = _context.ZoneVersions.Find(versionId);

            if (version == null) return;

            foreach (var z in zones)
            {
                // Nếu diện tích chưa có, tính thử từ Points
                if (z.Area <= 0 && !string.IsNullOrEmpty(z.Points))
                {
                    z.Area = ComputeArea(z.Points);
                }
            }

            version.SoPolygon = zones.Count();
            version.DienTichBaoPhu = zones.Sum(z => z.Area);
            version.UpdatedDate = DateTime.Now;
        }

        private double ComputeArea(string pointsJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(pointsJson);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Array) return 0;

                var points = new List<(double lat, double lng)>();
                foreach (var p in root.EnumerateArray())
                {
                    if (p.ValueKind == JsonValueKind.Array && p.GetArrayLength() >= 2)
                    {
                        points.Add((p[0].GetDouble(), p[1].GetDouble()));
                    }
                    else if (p.TryGetProperty("lat", out var latProp) && p.TryGetProperty("lng", out var lngProp))
                    {
                        points.Add((latProp.GetDouble(), lngProp.GetDouble()));
                    }
                }

                if (points.Count < 3) return 0;

                // Công thức Shoelace trên mặt phẳng (xấp xỉ gần đúng cho diện tích nhỏ)
                double area = 0;
                double latRef = points[0].lat;
                double lngRef = points[0].lng;

                for (int i = 0; i < points.Count; i++)
                {
                    int next = (i + 1) % points.Count;

                    // Chuyển sang mét (xấp xỉ)
                    double x1 = (points[i].lng - lngRef) * 111320 * Math.Cos(latRef * Math.PI / 180);
                    double y1 = (points[i].lat - latRef) * 111320;
                    double x2 = (points[next].lng - lngRef) * 111320 * Math.Cos(latRef * Math.PI / 180);
                    double y2 = (points[next].lat - latRef) * 111320;

                    area += (x1 * y2 - x2 * y1);
                }

                return Math.Abs(area) / 2.0;
            }
            catch { return 0; }
        }

        [HttpPost]
        public IActionResult RecalculateStats(int versionId)
        {
            UpdateVersionStats(versionId);
            _context.SaveChanges();
            return Json(new { success = true });
        }
        [HttpPost]
        public IActionResult SaveZone([FromBody] Zone data)
        {
            var zone = new Zone
            {
                ZoneName = data.ZoneName,
                AreaName = data.AreaName,
                Points = data.Points,
                CenterLat = data.CenterLat,
                CenterLng = data.CenterLng,
                VersionId = data.VersionId,
                Area = data.Area,
                CreatedAt = DateTime.Now
            };

            _context.Zones.Add(zone);
            _context.SaveChanges();
            if (zone.VersionId != null)
            {
                UpdateVersionStats(zone.VersionId.Value);
            }

            _context.SaveChanges();

            return Json(new { success = true });
        }
        // update vùng
        [HttpPost]
        public IActionResult UpdateZone([FromBody] Zone zone)
        {
            var z = _context.Zones.FirstOrDefault(x => x.Id == zone.Id);

            if (z == null)
                return Json(new { success = false });

            z.ZoneName = zone.ZoneName;
            z.Points = zone.Points;
            z.CenterLat = zone.CenterLat;
            z.CenterLng = zone.CenterLng;
            z.Area = zone.Area;
            _context.SaveChanges();
            if (zone.VersionId != null)
            {
                UpdateVersionStats(zone.VersionId.Value);
            }

            _context.SaveChanges();
            return Json(new { success = true });
        }

        // xóa vùng
        [HttpPost]
        public IActionResult DeleteZone(int id)
        {
            var zone = _context.Zones.FirstOrDefault(x => x.Id == id);

            if (zone == null)
                return Json(new { success = false });

            _context.Zones.Remove(zone);
            _context.SaveChanges();
            if (zone.VersionId != null)
            {
                UpdateVersionStats(zone.VersionId.Value);
            }

            _context.SaveChanges();
            return Json(new { success = true });
        }

        // xóa toàn bộ vùng theo version
        [HttpPost]
        public IActionResult DeleteAll(int versionId)
        {
            var zones = _context.Zones
                .Where(z => z.VersionId == versionId)
                .ToList();

            _context.Zones.RemoveRange(zones);
            _context.SaveChanges();
            UpdateVersionStats(versionId);
            _context.SaveChanges();
            return Json(new { success = true });
        }
        [HttpPost]
        public IActionResult UpdateArea([FromBody] dynamic data)
        {
            int zoneId = data.zoneId;
            int areaId = data.areaId;

            var zone = _context.Zones.Find(zoneId);

            // nếu bạn lưu AreaName
            var area = _context.Areas.Find(areaId);

            zone.AreaName = area.Name;

            _context.SaveChanges();

            return Json(new { success = true });
        }
        public IActionResult GetAll()
        {
            var data = _context.Areas
                .Select(a => new
                {
                    id = a.Id,
                    areaName = a.Name
                })
                .ToList();

            return Json(data);
        }

        // Lấy lịch sử zone (thực tế + dự kiến)
        [HttpGet]
        public IActionResult ZoneHistory(int zoneId)
        {
            AutoUpdateReal(); // 👈 thêm dòng này

            var data = _context.ZoneHistories
                .Where(z => z.ZoneId == zoneId)
                .OrderBy(z => z.Year)
                .ThenBy(z => z.Month)
                .Select(z => new
                {
                    month = z.Month,
                    year = z.Year,
                    ordersReal = z.OrdersReal,
                    customersReal = z.CustomersReal,
                    ordersForecast = z.OrdersForecast,
                    customersForecast = z.CustomersForecast
                })
                .ToList();

            return Json(data);
        }
        // Dự đoán đơn giản (moving average 3 tháng gần nhất)
        [HttpGet]
        
        public IActionResult Predict(int zoneId)
        {
            var data = _context.ZoneHistories
                .Where(x => x.ZoneId == zoneId && x.OrdersReal != null)
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .Take(3)
                .ToList();

            if (data.Count < 3)
                return Json(new { order = 0, customer = 0 });

            // trọng số: gần nhất -> quan trọng nhất
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
                customer = (int)Math.Round(customerPredict)
            });
        }

        // Lưu dự kiến cho tháng tiếp theo
        [HttpPost]
        public IActionResult SaveForecast([FromBody] ForecastDto dto)
        {
            var item = _context.ZoneHistories
                .FirstOrDefault(x => x.ZoneId == dto.ZoneId
                                  && x.Month == dto.Month
                                  && x.Year == dto.Year);

            if (item == null)
            {
                item = new ZoneHistory
                {
                    ZoneId = dto.ZoneId,
                    Month = dto.Month,
                    Year = dto.Year
                };
                _context.ZoneHistories.Add(item);
            }

            item.OrdersForecast = dto.OrdersForecast;
            item.CustomersForecast = dto.CustomersForecast;

            _context.SaveChanges();

            return Ok();
        }
        [HttpGet]
        public IActionResult GetRoute(int zoneId, int month, int year)
        {
            var data = _context.ZoneRoutes
                .Where(x => x.ZoneId == zoneId
                         && x.Month == month
                         && x.Year == year)
                .Select(x => new {
                    lat = x.Lat,
                    lng = x.Lng
                })
                .ToList();

            return Json(data);
        }
        public class ForecastDto
        {
            public int ZoneId { get; set; }
            public int OrdersForecast { get; set; }
            public int CustomersForecast { get; set; }
            public int Month { get; set; }
            public int Year { get; set; }   
        }
        public void AutoUpdateReal()
        {
            var now = DateTime.Now;
            int lastMonth = now.Month == 1 ? 12 : now.Month - 1;
            int year = now.Month == 1 ? now.Year - 1 : now.Year;

            var data = _context.ZoneHistories
                .Where(x => x.Month == lastMonth && x.Year == year)
                .ToList();

            var rand = new Random();

            foreach (var item in data)
            {
                if (item.OrdersReal == null)
                {
                    // tạo sai lệch ±20%
                    double factor = 0.8 + rand.NextDouble() * 0.4;

                    item.OrdersReal = (int)(item.OrdersForecast * factor);
                    item.CustomersReal = (int)(item.CustomersForecast * factor);
                }
            }

            _context.SaveChanges();
        }
    }
}

