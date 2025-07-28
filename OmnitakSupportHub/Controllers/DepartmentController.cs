using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;
using System.Threading.Tasks;

namespace OmnitakSupportHub.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class DepartmentController : Controller
    {
        private readonly OmnitakContext _context;
        public DepartmentController(OmnitakContext context) => _context = context;

        // GET: Department
        public async Task<IActionResult> Index()
        {
            var departments = await _context.Departments.ToListAsync();
            return View(departments);
        }

        // GET: Department/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Department/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Department department)
        {
            if (!ModelState.IsValid)
                return View(department);

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Department created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Department/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
                return NotFound();
            return View(department);
        }

        // POST: Department/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Department department)
        {
            if (id != department.DepartmentId)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(department);

            var dept = await _context.Departments.FindAsync(id);
            if (dept == null)
                return NotFound();

            dept.DepartmentName = department.DepartmentName;
            dept.Description = department.Description;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Department updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Department/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
                return NotFound();
            return View(department);
        }

        // POST: Department/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
                return NotFound();

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Department deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
