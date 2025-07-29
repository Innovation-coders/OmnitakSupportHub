using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;

namespace OmnitakSupportHub.Controllers
{
    [Authorize(Roles = "Administrator, Support Manager")]
    public class ITStaffController : Controller
    {
        private readonly OmnitakContext _context;

        public ITStaffController(OmnitakContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index() => View(await _context.ITStaff.ToListAsync());

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(ITStaff staff)
        {
            if (ModelState.IsValid)
            {
                _context.Add(staff);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(staff);
        }

        public async Task<IActionResult> Edit(int id) =>
            View(await _context.ITStaff.FindAsync(id));

        [HttpPost]
        public async Task<IActionResult> Edit(ITStaff staff)
        {
            _context.Update(staff);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id) =>
            View(await _context.ITStaff.FindAsync(id));

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var staff = await _context.ITStaff.FindAsync(id);
            _context.ITStaff.Remove(staff);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
