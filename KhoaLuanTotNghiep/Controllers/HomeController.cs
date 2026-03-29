using System.Diagnostics;
using KhoaLuanTotNghiep.Models;
using Microsoft.AspNetCore.Mvc;

namespace KhoaLuanTotNghiep.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Username") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.Role = HttpContext.Session.GetString("Role");
            return View();
        }
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None, Duration = 0)]
        public ActionResult AuthHome()
        {
            // Nếu là Admin hoặc Dispatcher -> Chuyển về Dashboard tương ứng
            var role = HttpContext.Session.GetString("Role");
            if (role == "Admin")       return RedirectToAction("Index", "Admin");
            if (role == "Dispatcher")  return RedirectToAction("Index", "Dispatcher");

            // Nếu là Driver hoặc chưa phân quyền thì dừng lại ở trang Landing Page (AuthHome)
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
