using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;
using OmnitakSupportHub.Services;
using OmnitakSupportHub.Models.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OmnitakSupportHub.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly IAuthService _authService;
        private readonly OmnitakContext _context;

        public AdminController(IAuthService authService, OmnitakContext context)
        {
            _authService = authService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> PendingUsers()
        {
            if (!User.HasClaim("CanApproveUsers", "true"))
            {
                return Forbid();
            }

            var pendingUsers = await _authService.GetPendingUsersAsync();
            var roles = await _context.Roles.ToListAsync();

            ViewBag.Roles = roles;
            return View(pendingUsers);
        }

        [HttpPost]
        [Authorize(Policy = "CanApproveUsers")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(int userId, int roleId, int departmentId)
        {
            if (!User.HasClaim("CanApproveUsers", "true"))
                return Forbid();

            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // Optional: include department update here too if your IAuthService handles it

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsApproved = true;
                user.IsActive = true;
                user.RoleID = roleId;
                user.DepartmentId = departmentId;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "User approved successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "User not found.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RejectUser(int userId)
        {
            if (!User.HasClaim("CanApproveUsers", "true"))
            {
                return Forbid();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "User rejected successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "User not found.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index");
            }

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"User {(user.IsActive ? "activated" : "deactivated")} successfully.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (!User.HasClaim("CanApproveUsers", "true"))
            {
                return Forbid();
            }

            var pendingCount = await _context.Users.CountAsync(u => !u.IsApproved && u.IsActive);
            var totalUsers = await _context.Users.CountAsync(u => u.IsActive);
            var totalTickets = await _context.Tickets.CountAsync();
            var openTickets = await _context.Tickets.CountAsync(t => t.Status.StatusName == "Open");

            ViewBag.PendingUsersCount = pendingCount;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalTickets = totalTickets;
            ViewBag.OpenTickets = openTickets;

            return View();
        }
        [HttpGet]
        [Authorize(Policy = "CanApproveUsers")]
        public async Task<IActionResult> Index()
        {
            var viewModel = new AdminDashboardViewModel
            {
                PendingUsers = await _context.Users
                    .Where(u => !u.IsApproved && u.IsActive)
                    .ToListAsync(),

                ActiveUsers = await _context.Users
                    .Where(u => u.IsApproved && u.IsActive)
                    .Include(u => u.Role)
                    .Include(u => u.Department)
                    .ToListAsync(),

                AvailableDepartments = await _context.Departments
                    .Select(d => new SelectListItem
                    {
                        Value = d.DepartmentId.ToString(),
                        Text = d.DepartmentName
                    }).ToListAsync(),

                AvailableRoles = await _context.Roles
                    .Select(r => new SelectListItem
                    {
                        Value = r.RoleID.ToString(),
                        Text = r.RoleName
                    }).ToListAsync(),

                TotalUsers = await _context.Users.CountAsync(),
                ActiveSessions = 0
            };

            return View(viewModel);
        }

    }
}

