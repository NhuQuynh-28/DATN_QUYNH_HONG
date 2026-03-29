using Microsoft.AspNetCore.Mvc;
using KhoaLuanTotNghiep.Data;
using KhoaLuanTotNghiep.Models;
using Microsoft.EntityFrameworkCore;

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

        // Danh sách user
        [HttpGet("")]
        public IActionResult Index()
        {
            var users = _context.Users.ToList();
            return View(users);
        }

        // Thêm user
        [HttpPost("Create")]
        public IActionResult Create(User user)
        {
            // Kiểm tra trùng username
            if (_context.Users.Any(u => u.Username == user.Username))
                return Json(new { success = false, message = "Username đã tồn tại!" });

            // Kiểm tra trùng email
            if (!string.IsNullOrEmpty(user.Email) && _context.Users.Any(u => u.Email == user.Email))
                return Json(new { success = false, message = "Email đã tồn tại!" });

            // Kiểm tra trùng CCCD
            if (!string.IsNullOrEmpty(user.Cccd) && _context.Users.Any(u => u.Cccd == user.Cccd))
                return Json(new { success = false, message = "CCCD đã tồn tại!" });

            user.CreatedAt = DateTime.Now;
            _context.Users.Add(user);
            _context.SaveChanges();

            // Tự động tạo hồ sơ Driver nếu role là Driver
            SyncDriverProfile(user);

            return Json(new { success = true });
        }

        // Xóa user
        [HttpPost("Delete")]
        public IActionResult Delete(int id)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }
            return Json(new { success = true });
        }

        // Lấy danh sách user
        [HttpGet("GetUsers")]
        public IActionResult GetUsers()
        {
            var users = _context.Users
                .Include(u => u.Area)
                .Select(u => new {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Phone,
                    u.Address,
                    u.Cccd,
                    u.Role,
                    u.IsApproved,
                    u.IsActive,
                    u.CreatedAt,
                    u.UpdatedAt,
                    areaName = u.Area != null ? u.Area.Name : null
                }).ToList();

            return Json(users);
        }

        // Approve - kiểm tra trùng username/email/cccd trước khi duyệt
        [HttpPost("Approve")]
        public IActionResult Approve(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            // Kiểm tra trùng username với user khác
            if (_context.Users.Any(u => u.Username == user.Username && u.Id != id))
                return Json(new { success = false, message = $"Username '{user.Username}' đã tồn tại trong hệ thống!" });

            // Kiểm tra trùng email
            if (!string.IsNullOrEmpty(user.Email) && _context.Users.Any(u => u.Email == user.Email && u.Id != id))
                return Json(new { success = false, message = $"Email '{user.Email}' đã tồn tại trong hệ thống!" });

            // Kiểm tra trùng CCCD
            if (!string.IsNullOrEmpty(user.Cccd) && _context.Users.Any(u => u.Cccd == user.Cccd && u.Id != id))
                return Json(new { success = false, message = $"CCCD '{user.Cccd}' đã tồn tại trong hệ thống!" });

            user.IsApproved = true;
            user.IsActive = true;
            user.UpdatedAt = DateTime.Now;
            _context.SaveChanges();

            // Tự động tạo hồ sơ Driver nếu role là Driver
            SyncDriverProfile(user);

            return Json(new { success = true });
        }

        // Reject - từ chối duyệt (không lưu DB)
        [HttpPost("Reject")]
        public IActionResult Reject(int id, string reason)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            // Không lưu lý do vào DB, chỉ trả về để hiển thị
            return Json(new { success = true, username = user.Username, reason = reason });
        }

        // Active/Deactive
        [HttpPost("ToggleActive")]
        public IActionResult ToggleActive(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.Now;
            _context.SaveChanges();

            return Json(new { success = true, isActive = user.IsActive });
        }

        // Đổi role
        [HttpPost("ChangeRole")]
        public IActionResult ChangeRole(int id, string role)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            user.Role = role;
            user.UpdatedAt = DateTime.Now;
            _context.SaveChanges();

            // Tự động tạo hồ sơ Driver nếu role là Driver
            SyncDriverProfile(user);

            return Json(new { success = true });
        }

        // Gán khu vực
        [HttpPost("AssignArea")]
        public IActionResult AssignArea(int id, int areaId)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            user.AreaId = areaId;
            user.UpdatedAt = DateTime.Now;
            _context.SaveChanges();

            return Json(new { success = true });
        }

        // Xem chi tiết
        [HttpGet("Detail")]
        public IActionResult Detail(int id)
        {
            var user = _context.Users
                .Include(u => u.Area)
                .Where(u => u.Id == id)
                .Select(u => new {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Phone,
                    u.Address,
                    u.Cccd,
                    u.Role,
                    u.IsApproved,
                    u.IsActive,
                    u.CreatedAt,
                    u.UpdatedAt,
                    areaName = u.Area != null ? u.Area.Name : null
                }).FirstOrDefault();

            return Json(user);
        }

        // Cập nhật thông tin user
        [HttpPost("Update")]
        public IActionResult Update(int id, string email, string phone, string address, string cccd, string role, int? areaId)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            // Kiểm tra trùng email
            if (!string.IsNullOrEmpty(email) && _context.Users.Any(u => u.Email == email && u.Id != id))
                return Json(new { success = false, message = "Email đã tồn tại!" });

            // Kiểm tra trùng CCCD
            if (!string.IsNullOrEmpty(cccd) && _context.Users.Any(u => u.Cccd == cccd && u.Id != id))
                return Json(new { success = false, message = "CCCD đã tồn tại!" });

            user.Email = email;
            user.Phone = phone;
            user.Address = address;
            user.Cccd = cccd;
            user.Role = role;
            user.AreaId = areaId;
            user.UpdatedAt = DateTime.Now;
            _context.SaveChanges();

            // Tự động tạo hồ sơ Driver nếu role là Driver
            SyncDriverProfile(user);

            return Json(new { success = true });
        }

        private void SyncDriverProfile(User user)
        {
            if (user.Role == "Driver")
            {
                // Kiểm tra xem đã có hồ sơ Driver với số điện thoại này chưa
                var driverExists = _context.Drivers.Any(d => d.Phone == user.Phone);
                if (!driverExists)
                {
                    var newDriver = new Driver
                    {
                        DriverName = user.Username,
                        Phone = user.Phone ?? "0000000000",
                        Status = "DangLam",
                        CreatedAt = DateTime.Now
                    };
                    _context.Drivers.Add(newDriver);
                    _context.SaveChanges();
                }
            }
        }
    }
}
