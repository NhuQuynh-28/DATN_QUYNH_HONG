using Microsoft.AspNetCore.Mvc;
using KhoaLuanTotNghiep.Data;
using KhoaLuanTotNghiep.Models;
using System.Linq;

namespace KhoaLuanTotNghiep.Controllers
{
    public class AreaController : Controller
    {
        private readonly AppDbContext _context;

        public AreaController(AppDbContext context)
        {
            _context = context;
        }

        // DANH SÁCH KHU VỰC
        public IActionResult Index()
        {
            var areas = _context.Areas.ToList();

            return PartialView("_AreaList", areas);
        }

        // FORM CREATE
        public IActionResult Create()
        {
            return PartialView("_CreateArea");
        }

        // SAVE CREATE
        [HttpPost]
        public IActionResult Create(Area area)
        {
            area.CreatedDate = DateTime.Now;

            _context.Areas.Add(area);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // DELETE
        public IActionResult Delete(int id)
        {
            var area = _context.Areas.Find(id);

            if (area != null)
            {
                _context.Areas.Remove(area);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}