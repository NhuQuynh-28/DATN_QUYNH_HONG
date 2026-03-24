using Microsoft.AspNetCore.Mvc;

namespace KhoaLuanTotNghiep.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
