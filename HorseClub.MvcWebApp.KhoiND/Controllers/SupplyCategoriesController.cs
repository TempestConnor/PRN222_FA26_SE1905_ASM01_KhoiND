using Microsoft.AspNetCore.Mvc;
using HorseClub.Services.KhoiND;

namespace HorseClub.MvcWebApp.KhoiND.Controllers
{
    // Read-only: lists rows, no create/edit/delete.
    public class SupplyCategoriesController : Controller
    {
        private readonly ISupplyCategoryServiceKhoiND _service;

        public SupplyCategoriesController(ISupplyCategoryServiceKhoiND service)
        {
            _service = service;
        }

        // GET: SupplyCategories
        public async Task<IActionResult> Index()
        {
            return View(await _service.GetAllAsync());
        }
    }
}
