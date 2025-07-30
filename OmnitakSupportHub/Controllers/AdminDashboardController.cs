using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmnitakSupportHub.Services;
using OmnitakSupportHub.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace OmnitakSupportHub.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminDashboardController : Controller
    {
        private readonly IAuthService _authService;
        private readonly OmnitakContext _context;

        public AdminDashboardController(IAuthService authService, OmnitakContext context)
        {
            _authService = authService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await _authService.GetAvailableRolesAsync();
            var departments = await _context.Departments.ToListAsync();

            var activeUsers = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.Role)
                .Where(u => u.IsActive)
                .ToListAsync();

            var pendingUsers = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.Role)
                .Where(u => !u.IsApproved)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                ActiveUsers = activeUsers,
                PendingUsers = pendingUsers,
                AvailableRoles = roles.Select(role => new SelectListItem
                {
                    Value = role.RoleID.ToString(),
                    Text = role.RoleName
                }).ToList(),
                AvailableDepartments = departments.Select(d => new SelectListItem
                {
                    Value = d.DepartmentId.ToString(),
                    Text = d.DepartmentName
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveUser(int userId, int roleId)
        {
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            var result = await _authService.ApproveUserAsync(userId, roleId, currentUserId);

            if (result)
            {
                TempData["SuccessMessage"] = "User approved successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to approve user.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RejectUser(int userId)
        {
            var result = await _authService.RejectUserAsync(userId);

            if (result)
            {
                TempData["SuccessMessage"] = "User rejected successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to reject user.";
            }

            return RedirectToAction("Index");
        }

        // GET: AdminDashboard/EditUser/5
        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.UserID == id);

            if (user == null)
                return NotFound();

            var departments = await _context.Departments.ToListAsync();
            var roles = await _context.Roles.ToListAsync();

            var model = new EditUserViewModel
            {
                UserId = user.UserID,
                FullName = user.FullName,
                DepartmentId = user.DepartmentId,
                RoleId = user.RoleID ?? 0,
                TeamId = user.TeamID,
                AvailableDepartments = departments.Select(d => new SelectListItem
                {
                    Value = d.DepartmentId.ToString(),
                    Text = d.DepartmentName
                }).ToList(),
                AvailableRoles = roles.Select(r => new SelectListItem
                {
                    Value = r.RoleID.ToString(),
                    Text = r.RoleName
                }).ToList()
            };

            return View(model);
        }

        // POST: AdminDashboard/EditUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload dropdowns if validation fails
                model.AvailableDepartments = (await _context.Departments.ToListAsync())
                    .Select(d => new SelectListItem { Value = d.DepartmentId.ToString(), Text = d.DepartmentName }).ToList();
                model.AvailableRoles = (await _context.Roles.ToListAsync())
                    .Select(r => new SelectListItem { Value = r.RoleID.ToString(), Text = r.RoleName }).ToList();
                return View(model);
            }

            var user = await _context.Users.FindAsync(model.UserId);
            if (user == null)
                return NotFound();

            user.FullName = model.FullName;
            user.DepartmentId = model.DepartmentId;
            user.RoleID = model.RoleId;
            user.TeamID = model.TeamId;

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // POST: AdminDashboard/ToggleUserStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            int performedById = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            bool success = await _authService.ToggleUserActiveAsync(id, performedById);

            if (!success)
                TempData["ErrorMessage"] = "User not found or could not be updated.";
            else
                TempData["SuccessMessage"] = "User status toggled successfully.";

            return RedirectToAction("Index");
        }
    }
}