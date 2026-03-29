using Microsoft.AspNetCore.Mvc;
using KhoaLuanTotNghiep.Data;
using KhoaLuanTotNghiep.Models;
using Microsoft.EntityFrameworkCore;

namespace KhoaLuanTotNghiep.Controllers
{
    public class AreaController : Controller
    {
        private readonly AppDbContext _context;

        public AreaController(AppDbContext context)
        {
            _context = context;
        }

        // Danh sách khu vực (partial view)
        public IActionResult Index()
        {
            var areas = _context.Areas.ToList();
            return PartialView("_AreaList", areas);
        }

        // Lấy tất cả khu vực dạng JSON (dùng cho sidebar dynamic + User assign)
        [HttpGet]
        public IActionResult GetAll()
        {
            var areas = _context.Areas
                .Select(a => new {
                    a.Id,
                    areaName = a.Name,
                    a.Status,
                    createdDate = a.CreatedDate.ToString("dd/MM/yyyy")
                }).ToList();
            return Json(areas);
        }

        // Thêm khu vực mới (JSON response)
        [HttpPost]
        public IActionResult Create([FromBody] Area area)
        {
            if (string.IsNullOrWhiteSpace(area.Name))
                return Json(new { success = false, message = "Tên khu vực không được để trống!" });

            if (_context.Areas.Any(a => a.Name == area.Name))
                return Json(new { success = false, message = "Tên khu vực đã tồn tại!" });

            area.Status = "Active";
            area.CreatedDate = DateTime.Now;
            _context.Areas.Add(area);
            _context.SaveChanges();

            return Json(new { success = true, id = area.Id, name = area.Name });
        }

        // Thêm khu vực (form POST - giữ lại cho PartialView cũ)
        [HttpPost]
        [Route("Area/CreateForm")]
        public IActionResult CreateForm(Area area)
        {
            area.CreatedDate = DateTime.Now;
            area.Status = "Active";
            _context.Areas.Add(area);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        // Xóa khu vực (JSON response)
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var area = _context.Areas.Find(id);
            if (area == null) return Json(new { success = false, message = "Không tìm thấy khu vực!" });

            _context.Areas.Remove(area);
            _context.SaveChanges();
            return Json(new { success = true });
        }

        // Xóa khu vực (GET - giữ tương thích)
        [HttpGet]
        [Route("Area/DeleteGet")]
        public IActionResult DeleteGet(int id)
        {
            var area = _context.Areas.Find(id);
            if (area != null)
            {
                _context.Areas.Remove(area);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // Cập nhật trạng thái khu vực
        [HttpPost]
        public IActionResult ToggleStatus(int id)
        {
            var area = _context.Areas.Find(id);
            if (area == null) return Json(new { success = false });
            area.Status = area.Status == "Active" ? "Inactive" : "Active";
            _context.SaveChanges();
            return Json(new { success = true, status = area.Status });
        }
    }
}