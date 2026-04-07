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
                .Where(z => z.VersionId == versionId);

            var version = _context.ZoneVersions.Find(versionId);

            if (version == null) return;

            version.SoPolygon = zones.Count();
            version.DienTichBaoPhu = zones.Sum(z => z.Area);
            version.UpdatedDate = DateTime.Now;
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
        [HttpPost]
        public IActionResult CalculateDistanceMatrix(int versionId)
        {
            var zones = _context.Zones.Where(z => z.VersionId == versionId).ToList();
            if (zones.Count == 0) return Json(new { success = false, message = "Không tìm thấy vùng nào trong version này." });

            // 1. Xóa dữ liệu cũ của các vùng thuộc version này
            var zoneIds = zones.Select(z => z.Id).ToList();
            var oldDistances = _context.ZoneDistances
                .Where(d => zoneIds.Contains(d.ZoneId1) || zoneIds.Contains(d.ZoneId2))
                .ToList();
            
            if (oldDistances.Any())
            {
                _context.ZoneDistances.RemoveRange(oldDistances);
                _context.SaveChanges();
            }

            // 2. Tính toán ma trận
            var newDistances = new List<ZoneDistance>();
            for (int i = 0; i < zones.Count; i++)
            {
                for (int j = i + 1; j < zones.Count; j++)
                {
                    var z1 = zones[i];
                    var z2 = zones[j];

                    double dist = CalculateHaversine(z1.CenterLat, z1.CenterLng, z2.CenterLat, z2.CenterLng);
                    bool isAdj = CheckAdjacencyBB(z1.Points, z2.Points);

                    newDistances.Add(new ZoneDistance
                    {
                        ZoneId1 = z1.Id,
                        ZoneId2 = z2.Id,
                        Distance = Math.Round(dist, 2),
                        IsAdjacent = isAdj
                    });
                }
            }

            _context.ZoneDistances.AddRange(newDistances);
            _context.SaveChanges();

            return Json(new { success = true, count = newDistances.Count });
        }

        private double CalculateHaversine(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371000; // Meters
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

        private bool CheckAdjacencyBB(string points1, string points2)
        {
            if (string.IsNullOrEmpty(points1) || string.IsNullOrEmpty(points2)) return false;
            try
            {
                var b1 = GetBB(points1);
                var b2 = GetBB(points2);
                if (b1 == null || b2 == null) return false;

                // Kiểm tra giao nhau của Bounding Box
                return !(b2.minLat > b1.maxLat || b2.maxLat < b1.minLat || b2.minLng > b1.maxLng || b2.maxLng < b1.minLng);
            }
            catch { return false; }
        }

        private dynamic GetBB(string json)
        {
            try
            {
                if (string.IsNullOrEmpty(json)) return null;
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Array) return null;

                double minLat = 90, maxLat = -90, minLng = 180, maxLng = -180;
                bool hasData = false;

                foreach (var p in root.EnumerateArray())
                {
                    double lat = 0, lng = 0;
                    bool found = false;

                    // Handle [[lat, lng], ...]
                    if (p.ValueKind == JsonValueKind.Array && p.GetArrayLength() >= 2)
                    {
                        lat = p[0].GetDouble();
                        lng = p[1].GetDouble();
                        found = true;
                    }
                    // Handle [{lat: 1, lng: 1}, ...] or [{Lat: 1, Lng: 1}, ...]
                    else if (p.ValueKind == JsonValueKind.Object)
                    {
                        if (p.TryGetProperty("lat", out var latProp) || p.TryGetProperty("Lat", out latProp))
                        {
                            lat = latProp.GetDouble();
                            if (p.TryGetProperty("lng", out var lngProp) || p.TryGetProperty("Lng", out lngProp) ||
                                p.TryGetProperty("lon", out lngProp) || p.TryGetProperty("Lon", out lngProp))
                            {
                                lng = lngProp.GetDouble();
                                found = true;
                            }
                        }
                    }

                    if (found)
                    {
                        if (lat < minLat) minLat = lat; if (lat > maxLat) maxLat = lat;
                        if (lng < minLng) minLng = lng; if (lng > maxLng) maxLng = lng;
                        hasData = true;
                    }
                }
                return hasData ? new { minLat, maxLat, minLng, maxLng } : null;
            }
            catch { return null; }
        }
    }
}

