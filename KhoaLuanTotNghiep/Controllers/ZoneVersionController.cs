using Microsoft.AspNetCore.Mvc;
using KhoaLuanTotNghiep.Data;
using System.Linq;
using KhoaLuanTotNghiep.Models;
using Microsoft.EntityFrameworkCore;

namespace KhoaLuanTotNghiep.Controllers
{
    public class ZoneVersionController : Controller
    {
        private readonly AppDbContext _context;

        public ZoneVersionController(AppDbContext context)
        {
            _context = context;
        }

        // 👉 load danh sách version (Partial)
        public IActionResult Index(int areaId)
        {
            var versions = _context.ZoneVersions
                .Include(v => v.Zones)
                .Where(v => v.AreaId == areaId)
                .ToList();

            ViewBag.AreaId   = areaId;
            ViewBag.AreaName = _context.Areas.Find(areaId)?.Name ?? "";

            return PartialView("_VersionList", versions);
        }

        // 👉 mở form create
        public IActionResult Create(int areaId)
        {
            ViewBag.AreaId = areaId;
            return PartialView("_CreateVersion");
        }

        // 👉 submit create (AJAX)
        [HttpPost]
        public IActionResult Create(ZoneVersion version)
        {
            version.CreatedDate = DateTime.Now;
            version.UpdatedDate = DateTime.Now;
            version.SoPolygon = 0;
            version.DienTichBaoPhu = 0;

            if (version.IsActive == true)
            {
                var versions = _context.ZoneVersions
                    .Where(v => v.AreaId == version.AreaId);

                foreach (var v in versions)
                {
                    v.IsActive = false;
                }
            }

            _context.ZoneVersions.Add(version);
            _context.SaveChanges();

            // 👉 trả lại list mới (QUAN TRỌNG)
            var list = _context.ZoneVersions
                .Include(v => v.Zones)
                .Where(v => v.AreaId == version.AreaId)
                .ToList();

            ViewBag.AreaId   = version.AreaId;
            ViewBag.AreaName = _context.Areas.Find(version.AreaId)?.Name ?? "";

            return PartialView("_VersionList", list);
        }

        // 👉 active version
        public IActionResult SetActive(int id)
        {
            var version = _context.ZoneVersions.Find(id);

            var versions = _context.ZoneVersions
                .Where(v => v.AreaId == version.AreaId);

            foreach (var v in versions)
            {
                v.IsActive = false;
            }

            version.IsActive = true;
            version.UpdatedDate = DateTime.Now;

            _context.SaveChanges();

            return Ok();
        }

        // 👉 delete
        public IActionResult Delete(int id)
        {
            var version = _context.ZoneVersions.Find(id);

            if (version != null)
            {
                int areaId = version.AreaId ?? 0;

                _context.ZoneVersions.Remove(version);
                _context.SaveChanges();

                var list = _context.ZoneVersions
                    .Include(v => v.Zones)
                    .Where(v => v.AreaId == areaId)
                    .ToList();

                ViewBag.AreaId   = areaId;
                ViewBag.AreaName = _context.Areas.Find(areaId)?.Name ?? "";

                return PartialView("_VersionList", list);
            }

            return NotFound();
        }
        public IActionResult GetActiveVersion(int areaId)
        {
            var v = _context.ZoneVersions
                .FirstOrDefault(x => x.AreaId == areaId && x.IsActive == true);

            return Json(v);
        }
        public IActionResult GetByArea(int areaId)
        {
            var list = _context.ZoneVersions
                .Where(v => v.AreaId == areaId)
                .Select(v => new {
                    id = v.Id,
                    versionName = v.VersionName,
                    isActive = v.IsActive
                })
                .ToList();

            return Json(list);
        }
    }
}