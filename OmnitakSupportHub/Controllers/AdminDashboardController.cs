using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmnitakSupportHub.Services;
using OmnitakSupportHub.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OmnitakSupportHub.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminDashboardController : Controller
    {
        private readonly IAuthService _authService;

        public AdminDashboardController(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await _authService.GetAvailableRolesAsync();
            var selectListRoles = roles.Select(role => new SelectListItem
            {
                Value = role.RoleID.ToString(),
                Text = role.RoleName
            }).ToList();

            var model = new OmnitakSupportHub.Models.ViewModels.AdminDashboardViewModel
            {
                PendingUsers = await _authService.GetPendingUsersAsync(),
                AvailableRoles = selectListRoles,
                ActiveUsers = await _authService.GetActiveUsersAsync()
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
            var user = await _authService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            var roles = await _authService.GetAvailableRolesAsync();

            var model = new EditUserViewModel
            {
                UserId = user.UserID,
                FullName = user.FullName,
                Department = user.Department?.DepartmentName,
                RoleId = user.RoleID ?? 0,
                TeamId = user.TeamID,
                AvailableRoles = roles
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
                model.AvailableRoles = await _authService.GetAvailableRolesAsync();
                return View(model);
            }

            int modifiedById = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            bool result = await _authService.UpdateUserAsync(
                model.UserId,
                model.FullName,
                model.Department,
                model.RoleId,
                model.TeamId,
                modifiedById
            );

            if (result)
                TempData["SuccessMessage"] = "User details updated successfully.";
            else
                TempData["ErrorMessage"] = "Failed to update user details.";

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