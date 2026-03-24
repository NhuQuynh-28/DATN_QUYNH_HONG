using Microsoft.AspNetCore.Mvc;

namespace KhoaLuanTotNghiep.Controllers
{
    public class DispatcherController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

    }
}
