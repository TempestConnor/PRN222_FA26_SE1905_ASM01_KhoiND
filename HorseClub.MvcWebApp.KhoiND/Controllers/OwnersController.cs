using Microsoft.AspNetCore.Mvc;
using HorseClub.Services.KhoiND;

namespace HorseClub.MvcWebApp.KhoiND.Controllers
{
    // Read-only: lists rows, no create/edit/delete.
    public class OwnersController : Controller
    {
        private readonly IOwnerServiceKhoiND _service;

        public OwnersController(IOwnerServiceKhoiND service)
        {
            _service = service;
        }

        // GET: Owners
        public async Task<IActionResult> Index()
        {
            return View(await _service.GetAllAsync());
        }
    }
}
