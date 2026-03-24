using Microsoft.AspNetCore.Mvc;
using KhoaLuanTotNghiep.Data;
using KhoaLuanTotNghiep.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Net;
using System.Net.Mail;

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
            var exists = _context.Users
                .FirstOrDefault(x => x.Username == user.Username);

            if (exists != null)
            {
                ViewBag.Error = "Username đã tồn tại";
                return View();
            }

            // ⭐ mặc định role
            user.Role = "Driver";

            // ⭐ QUAN TRỌNG: chờ admin duyệt
            user.IsApproved = false;

            // ⭐ cho phép hoạt động
            user.IsActive = true;

            _context.Users.Add(user);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }

        //--------------------------------
        // LOGIN
        //--------------------------------
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        // Đăng nhập
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var user = _context.Users
                .FirstOrDefault(x =>
                    x.Username == username &&
                    x.Password == password);

            if (user == null)
            {
                ViewBag.Error = "Sai tài khoản hoặc mật khẩu";
                return View();
            }

            // ⭐ CHƯA DUYỆT → CHẶN
            if (!user.IsApproved)
            {
                ViewBag.Error = "Tài khoản đang chờ admin duyệt";
                return View();
            }

            // ⭐ BỊ KHÓA → CHẶN
            if (!user.IsActive)
            {
                ViewBag.Error = "Tài khoản đã bị khóa";
                return View();
            }

            // ⭐ LƯU SESSION
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);

            // ⭐ PHÂN QUYỀN
            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }
            else if (user.Role == "Dispatcher")
            {
                return RedirectToAction("Index", "Dispatcher");
            }
            else if (user.Role == "Driver")
            {
                return RedirectToAction("Index", "Zone");
            }

            // fallback
            return RedirectToAction("Login");
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

            SendEmail(user.Email, code);

            ViewBag.Message = "Mã reset đã gửi về email";

            return RedirectToAction("ResetPassword");
        }
        public void SendEmail(string toEmail, string code)
        {
            var fromEmail = "buithinhuquynh553@gmail.com";   // email gửi
            var fromPassword = "ystm kzmd suld zojz";

            MailMessage message = new MailMessage();

            message.From = new MailAddress(fromEmail);
            message.To.Add(new MailAddress(toEmail));

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

            user.Password = newPassword;
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

            return RedirectToAction("Login");
        }
    }
}