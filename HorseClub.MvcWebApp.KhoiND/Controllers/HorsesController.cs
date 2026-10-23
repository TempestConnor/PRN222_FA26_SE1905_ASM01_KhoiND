using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using HorseClub.Entities.KhoiND.Enums;
using HorseClub.Entities.KhoiND.Models;
using HorseClub.Services.KhoiND;

namespace HorseClub.MvcWebApp.KhoiND.Controllers
{
    public class HorsesController : Controller
    {
        // Columns a form may post. IsDeleted/DeletedAt/UpdatedAt are set by the service only.
        private const string CreateFields =
            "Name,MicrochipNo,PassportNo,Sex,Color,Breed,BirthDate,Status,OwnerId,SireId,DamId";
        private const string EditFields = "Id,CreatedAt,CreatedBy," + CreateFields;

        private readonly IHorseServiceKhoiND _service;
        private readonly IOwnerServiceKhoiND _ownerService;

        public HorsesController(IHorseServiceKhoiND service, IOwnerServiceKhoiND ownerService)
        {
            _service = service;
            _ownerService = ownerService;
        }

        // GET: Horses?name=..&breed=..&ownerName=..
        public async Task<IActionResult> Index(string? name, string? breed, string? ownerName)
        {
            ViewData["Name"] = name;
            ViewData["Breed"] = breed;
            ViewData["OwnerName"] = ownerName;
            var items = await _service.GetAllAsync(name, breed, ownerName);
            return View(items);
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();
            var item = await _service.GetByIdAsync(id.Value);
            if (item == null) return NotFound();
            return View(item);
        }

        public async Task<IActionResult> Create()
        {
            var item = new Horse { BirthDate = DateOnly.FromDateTime(DateTime.Today) };
            await LoadSelectListsAsync(item);
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind(CreateFields)] Horse item)
        {
            RemoveNavigationErrors();
            if (ModelState.IsValid)
            {
                try
                {
                    await _service.CreateAsync(item);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.GetBaseException().Message);
                }
            }
            await LoadSelectListsAsync(item);
            return View(item);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            var item = await _service.GetByIdAsync(id.Value);
            if (item == null) return NotFound();
            await LoadSelectListsAsync(item);
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind(EditFields)] Horse item)
        {
            if (id != item.Id) return NotFound();
            RemoveNavigationErrors();
            if (ModelState.IsValid)
            {
                try
                {
                    // Don't let EF touch the related rows.
                    item.Owner = null;
                    item.Sire = null;
                    item.Dam = null;
                    await _service.UpdateAsync(item);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.GetBaseException().Message);
                }
            }
            await LoadSelectListsAsync(item);
            return View(item);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();
            var item = await _service.GetByIdAsync(id.Value);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewData["Error"] = ex.GetBaseException().Message;
                var item = await _service.GetByIdAsync(id);
                return item == null ? RedirectToAction(nameof(Index)) : View(item);
            }
        }

        // Navigations are never posted.
        private void RemoveNavigationErrors()
        {
            ModelState.Remove(nameof(Horse.Owner));
            ModelState.Remove(nameof(Horse.Sire));
            ModelState.Remove(nameof(Horse.Dam));
        }

        private async Task LoadSelectListsAsync(Horse item)
        {
            ViewData["OwnerId"] = new SelectList(
                await _ownerService.GetAllAsync(), nameof(Owner.Id), nameof(Owner.FullName), item.OwnerId);

            // A horse can't be its own parent; sires are male, dams are mares.
            var others = (await _service.GetAllAsync()).Where(h => h.Id != item.Id).ToList();
            ViewData["SireId"] = new SelectList(
                others.Where(h => h.Sex != (byte)HorseSex.Mare), nameof(Horse.Id), nameof(Horse.Name), item.SireId);
            ViewData["DamId"] = new SelectList(
                others.Where(h => h.Sex == (byte)HorseSex.Mare), nameof(Horse.Id), nameof(Horse.Name), item.DamId);

            ViewData["Sex"] = EnumSelectList<HorseSex>(item.Sex);
            ViewData["Status"] = EnumSelectList<HorseStatus>(item.Status);
        }

        private static SelectList EnumSelectList<TEnum>(byte selected) where TEnum : struct, Enum
        {
            var options = Enum.GetValues<TEnum>()
                .Select(e => new { Value = Convert.ToByte(e), Text = e.ToString() });
            return new SelectList(options, "Value", "Text", selected);
        }
    }
}
