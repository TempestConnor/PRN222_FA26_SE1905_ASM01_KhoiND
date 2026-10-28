using Microsoft.AspNetCore.Mvc;
using HorseClub.Services.KhoiND;

namespace HorseClub.MvcWebApp.KhoiND.Controllers
{
    // Read-only: lists rows, no create/edit/delete.
    public class StaffProfilesController : Controller
    {
        private readonly IStaffProfileServiceKhoiND _service;

        public StaffProfilesController(IStaffProfileServiceKhoiND service)
        {
            _service = service;
        }

        // GET: StaffProfiles
        public async Task<IActionResult> Index()
        {
            return View(await _service.GetAllAsync());
        }
    }
}
