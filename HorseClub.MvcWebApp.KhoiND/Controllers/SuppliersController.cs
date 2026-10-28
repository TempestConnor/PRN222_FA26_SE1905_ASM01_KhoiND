using Microsoft.AspNetCore.Mvc;
using HorseClub.Services.KhoiND;

namespace HorseClub.MvcWebApp.KhoiND.Controllers
{
    // Read-only: lists rows, no create/edit/delete.
    public class SuppliersController : Controller
    {
        private readonly ISupplierServiceKhoiND _service;

        public SuppliersController(ISupplierServiceKhoiND service)
        {
            _service = service;
        }

        // GET: Suppliers
        public async Task<IActionResult> Index()
        {
            return View(await _service.GetAllAsync());
        }
    }
}
