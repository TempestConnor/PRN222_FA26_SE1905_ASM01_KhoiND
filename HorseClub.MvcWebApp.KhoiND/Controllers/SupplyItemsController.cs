using Microsoft.AspNetCore.Mvc;
using HorseClub.Services.KhoiND;

namespace HorseClub.MvcWebApp.KhoiND.Controllers
{
    // Read-only: lists rows, no create/edit/delete.
    public class SupplyItemsController : Controller
    {
        private readonly ISupplyItemServiceKhoiND _service;

        public SupplyItemsController(ISupplyItemServiceKhoiND service)
        {
            _service = service;
        }

        // GET: SupplyItems
        public async Task<IActionResult> Index()
        {
            return View(await _service.GetAllAsync());
        }
    }
}
