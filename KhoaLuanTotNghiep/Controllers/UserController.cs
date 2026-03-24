using Microsoft.AspNetCore.Mvc;
using KhoaLuanTotNghiep.Data;
using KhoaLuanTotNghiep.Models;

namespace KhoaLuanTotNghiep.Controllers
{
    [Route("User")]
    public class UserController : Controller
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        // danh sách user
        [HttpGet("")]
        public IActionResult Index()
        {
            var users = _context.Users.ToList();
            return View(users);
        }

        // thêm user
        [HttpPost("Create")]
        public IActionResult Create(User user)
        {
            _context.Users.Add(user);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // xóa user
        [HttpPost("Delete")]
        public IActionResult Delete(int id)
        {
            var user = _context.Users.Find(id);

            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
            // 🔹 Lấy danh sách user
            [HttpGet("GetUsers")]
            public IActionResult GetUsers()
            {
                var users = _context.Users
                    .Select(u => new {
                        u.Id,
                        u.Username,
                        u.Role,
                        u.IsApproved,
                        u.IsActive,
                        areaName = u.Area != null ? u.Area.Name : null
                    }).ToList();

                return Json(users);
            }

            // 🔹 Approve
            [HttpPost("Approve")]
            public IActionResult Approve(int id)
            {
                var user = _context.Users.Find(id);
                if (user == null) return NotFound();

                user.IsApproved = true;
                _context.SaveChanges();

                return Json(new { success = true });
            }

            // 🔹 Active/Deactive
            [HttpPost("ToggleActive")]
            public IActionResult ToggleActive(int id)
            {
                var user = _context.Users.Find(id);
                if (user == null) return NotFound();

                user.IsActive = !user.IsActive;
                _context.SaveChanges();

                return Json(new { success = true });
            }

            // 🔹 Đổi role
            [HttpPost("ChangeRole")]
            public IActionResult ChangeRole(int id, string role)
            {
                var user = _context.Users.Find(id);
                if (user == null) return NotFound();

                user.Role = role;
                _context.SaveChanges();

                return Json(new { success = true });
            }

            // 🔹 Gán khu vực
            [HttpPost("AssignArea")]
            public IActionResult AssignArea(int id, int areaId)
            {
                var user = _context.Users.Find(id);
                if (user == null) return NotFound();

                user.AreaId = areaId;
                _context.SaveChanges();

                return Json(new { success = true });
            }

            // 🔹 Xem chi tiết
            [HttpGet("Detail")]
            public IActionResult Detail(int id)
            {
                var user = _context.Users
                    .Where(u => u.Id == id)
                    .Select(u => new {
                        u.Id,
                        u.Username,
                        u.Role,
                        u.IsApproved,
                        u.IsActive,
                        areaName = u.Area.Name
                    }).FirstOrDefault();

                return Json(user);
            }
        }
    }
