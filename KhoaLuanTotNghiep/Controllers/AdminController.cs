using Microsoft.AspNetCore.Mvc;

namespace KhoaLuanTotNghiep.Controllers
{
    public class AdminController : Controller
    {
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None, Duration = 0)]
        public IActionResult Index()
        {
            return View();
        }
    }
}
