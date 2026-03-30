using Microsoft.AspNetCore.Mvc;
using KhoaLuanTotNghiep.Data;
using KhoaLuanTotNghiep.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System;
using System.IO;

namespace KhoaLuanTotNghiep.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        //--------------------------------
        // REGISTER
        //--------------------------------

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User user)
        {
            // ── Validate: Mật khẩu mạnh ─────────────────────────────────────
            var pwd = user.Password ?? "";
            if (pwd.Length < 7)
            {
                ViewBag.Error = "Mật khẩu phải có ít nhất 7 ký tự!";
                return View(user);
            }
            if (!Regex.IsMatch(pwd, @"[A-Z]"))
            {
                ViewBag.Error = "Mật khẩu phải chứa ít nhất 1 chữ cái in HOA!";
                return View(user);
            }
            if (!Regex.IsMatch(pwd, @"[0-9]"))
            {
                ViewBag.Error = "Mật khẩu phải chứa ít nhất 1 chữ số!";
                return View(user);
            }
            if (!Regex.IsMatch(pwd, @"[^a-zA-Z0-9]"))
            {
                ViewBag.Error = "Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt (vd: @, #, !, ...)!";
                return View(user);
            }

            // ── Validate: Username trùng ─────────────────────────────────────
            if (_context.Users.Any(x => x.Username == user.Username))
            {
                ViewBag.Error = "Tên đăng nhập đã tồn tại, vui lòng chọn tên khác!";
                return View(user);
            }

            // ── Validate: Email trùng ────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(user.Email) &&
                _context.Users.Any(x => x.Email == user.Email))
            {
                ViewBag.Error = "Email này đã được sử dụng!";
                return View(user);
            }

            // ── Validate: CCCD trùng ─────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(user.Cccd) &&
                _context.Users.Any(x => x.Cccd == user.Cccd))
            {
                ViewBag.Error = "CCCD này đã được đăng ký!";
                return View(user);
            }

            // ── Validate: Mật khẩu khớp ──────────────────────────────────────
            if (user.Password != user.ConfirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp!";
                return View(user);
            }

            // ── Mã hóa mật khẩu bằng SHA-256 trước khi lưu ──────────────────
            user.Password        = HashPassword(pwd);
            user.ConfirmPassword = user.Password;

            // ── Set defaults ─────────────────────────────────────────────────
            user.Role       = "Driver";
            user.IsApproved = false;
            user.IsActive   = true;
            user.CreatedAt  = DateTime.Now;
            user.UpdatedAt  = DateTime.Now;

            _context.Users.Add(user);
            _context.SaveChanges();

            TempData["RegisterSuccess"] = "Đăng ký thành công! Vui lòng đợi Admin phê duyệt tài khoản.";
            return RedirectToAction("Login");
        }

        //--------------------------------
        // LOGIN
        //--------------------------------
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None, Duration = 0)]
        public IActionResult Login()
        {
            // Nếu đã đăng nhập rồi → redirect về trang tương ứng
            var role = HttpContext.Session.GetString("Role");
            if (!string.IsNullOrEmpty(role))
            {
                if (role == "Admin")       return RedirectToAction("Index", "Admin");
                if (role == "Dispatcher")  return RedirectToAction("Index", "Dispatcher");
                if (role == "Driver")      return RedirectToAction("Index", "DriverDashboard");
            }
            return View();
        }
        // Đăng nhập
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!";
                ViewBag.Username = username;
                return View();
            }

            var hashedInput = HashPassword(password);
            var user = _context.Users
                .FirstOrDefault(x =>
                    x.Username == username &&
                    x.Password == hashedInput);

            if (user == null)
            {
                ViewBag.Error = "Sai tài khoản hoặc mật khẩu. Vui lòng kiểm tra lại!";
                ViewBag.Username = username;
                return View();
            }

            if (!user.IsApproved)
            {
                ViewBag.Error = "Tài khoản đang chờ Admin phê duyệt. Vui lòng thử lại sau!";
                ViewBag.Username = username;
                return View();
            }

            if (!user.IsActive)
            {
                ViewBag.Error = "Tài khoản đã bị khóa. Vui lòng liên hệ Admin!";
                ViewBag.Username = username;
                return View();
            }

            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);
            TempData["LoginSuccess"] = $"Chào mừng {user.Username} đã quay trở lại!";

            // ── Clean UX: Dùng location.replace để xóa trang Login khỏi lịch sử trình duyệt ──
            string? redirectUrl = user.Role switch
            {
                "Admin" => Url.Action("Index", "Admin"),
                "Dispatcher" => Url.Action("Index", "Dispatcher"),
                "Driver" => Url.Action("Index", "DriverDashboard"),
                _ => Url.Action("Login", "Account")
            };

            return Content($"<script>window.location.replace('{redirectUrl}');</script>", "text/html");
        }

        // GET: /Account/Profile – Show current user info
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None, Duration = 0)]
        public IActionResult Profile()
        {
            var username = HttpContext.Session.GetString("Username");
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return RedirectToAction("Login");

            if (user.Role == "Driver")
            {
                var driver = _context.Drivers.FirstOrDefault(d => d.Phone == user.Phone);
                ViewBag.Driver = driver;
            }

            return View(user);
        }

        // POST: /Account/EditProfile – Save edited info & avatar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(IFormFile avatar, [FromForm] User editedUser)
        {
            var username = HttpContext.Session.GetString("Username");
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return RedirectToAction("Login");

            // Update editable fields
            user.Email = editedUser.Email;
            user.Phone = editedUser.Phone;
            user.Address = editedUser.Address;
            user.Cccd = editedUser.Cccd;

            // Handle avatar upload
            if (avatar != null && avatar.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(avatar.FileName);
                var filePath = Path.Combine(uploads, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    avatar.CopyTo(stream);
                }
                user.AvatarUrl = $"/uploads/{fileName}";
            }

            _context.SaveChanges();
            
            // Sync with Driver profile if the user is a Driver
            if (user.Role == "Driver")
            {
                SyncDriverProfile(user);
            }

            TempData["Success"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction("Profile");
        }

        private void SyncDriverProfile(User user)
        {
            // Sync logic: Find by phone then update name, or create if not found
            var driver = _context.Drivers.FirstOrDefault(d => d.Phone == user.Phone);
            if (driver != null)
            {
                driver.DriverName = user.Username; // Or user.FullName if you have it
                _context.SaveChanges();
            }
            else
            {
                // Optionally create if phone was changed to something NEW that doesn't exist
                _context.Drivers.Add(new Driver 
                { 
                    DriverName = user.Username,
                    Phone = user.Phone ?? "0000000000",
                    Status = "DangLam",
                    CreatedAt = DateTime.Now
                });
                _context.SaveChanges();
            }
        }

        // Quên mk
        public ActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            var user = _context.Users.FirstOrDefault(x => x.Email != null && x.Email == email);

            if (user == null)
            {
                ViewBag.Message = "Email không tồn tại";
                return View();
            }

            // tạo mã reset
            string code = new Random().Next(100000, 999999).ToString();

            user.ResetCode = code;

            _context.SaveChanges();

            SendEmail(user.Email!, code);

            ViewBag.Message = "Mã reset đã gửi về email";

            return RedirectToAction("ResetPassword");
        }
        public void SendEmail(string? toEmail, string code)
        {
            var fromEmail = "buithinhuquynh553@gmail.com";   // email gửi
            var fromPassword = "ystm kzmd suld zojz";

            MailMessage message = new MailMessage();

            message.From = new MailAddress(fromEmail);
            message.To.Add(new MailAddress(toEmail!));

            message.Subject = "Khôi phục mật khẩu";

            message.Body =
                "Mã đặt lại mật khẩu của bạn là: " + code +
                "\n\nVào trang này để đổi mật khẩu.";

            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);

            smtp.Credentials = new NetworkCredential(fromEmail, fromPassword);

            smtp.EnableSsl = true;

            smtp.Send(message);
        }
        public IActionResult ResetPassword()
        {
            return View();
        }
        // Đổi mk
        [HttpPost]
        public IActionResult ResetPassword(string code, string newPassword)
        {
            var user = _context.Users.FirstOrDefault(x => x.ResetCode == code);

            if (user == null)
            {
                ViewBag.Message = "Mã không đúng";
                return View();
            }

            user.Password  = HashPassword(newPassword); // hash trước khi lưu
            user.ResetCode = null;

            _context.SaveChanges();

            ViewBag.Message = "Đổi mật khẩu thành công";

            return RedirectToAction("Login");
        }
        //--------------------------------
        // LOGOUT
        //--------------------------------
        // Đăng xuất
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            // Xóa cookie session nếu có
            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }
            
            // Ép trình duyệt không lưu cache trang này
            Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("Expires", "0");

            return RedirectToAction("Login");
        }

        //--------------------------------
        // SHA-256 PASSWORD HASH HELPER
        //--------------------------------
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}