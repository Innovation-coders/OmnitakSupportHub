using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;
using OmnitakSupportHub.Models.ViewModels;
using System.Security.Claims;

namespace OmnitakSupportHub.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class UserManagementController : Controller
    {
        private readonly OmnitakContext _context;

        public UserManagementController(OmnitakContext context)
        {
            _context = context;
        }

        // GET: UserManagement
        public async Task<IActionResult> Index()
        {
            var activeUsers = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.Role)
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var roles = await _context.Roles
                .OrderBy(r => r.RoleName)
                .ToListAsync();

            var model = new UserManagementViewModel
            {
                Users = activeUsers,
                AvailableRoles = roles.Select(r => new SelectListItem
                {
                    Value = r.RoleID.ToString(),
                    Text = r.RoleName
                }).ToList()
            };

            return View(model);
        }

        // POST: UserManagement/UpdateUserRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole(int userId, int roleId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Index");
                }

                var role = await _context.Roles.FindAsync(roleId);
                if (role == null)
                {
                    TempData["ErrorMessage"] = "Invalid role selected.";
                    return RedirectToAction("Index");
                }

                user.RoleID = roleId;
                await _context.SaveChangesAsync();

                // Log the action
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var auditLog = new AuditLog
                {
                    UserID = currentUserId,
                    Action = $"Updated user role for {user.FullName} to {role.RoleName}",
                    PerformedAt = DateTime.UtcNow
                };
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Successfully updated {user.FullName}'s role to {role.RoleName}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while updating the user role.";
                // Log the exception (consider using a logging framework)
                Console.WriteLine($"Error updating user role: {ex.Message}");
            }

            return RedirectToAction("Index");
        }

        // GET: UserManagement/CreateRole
        public IActionResult CreateRole()
        {
            return View(new CreateRoleViewModel());
        }

        // POST: UserManagement/CreateRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(CreateRoleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Check if role name already exists
                var existingRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.RoleName.ToLower() == model.RoleName.ToLower());

                if (existingRole != null)
                {
                    ModelState.AddModelError("RoleName", "A role with this name already exists.");
                    return View(model);
                }

                var newRole = new Role
                {
                    RoleName = model.RoleName,
                    Description = model.Description,
                    CanApproveUsers = model.CanApproveUsers,
                    CanManageTickets = model.CanManageTickets,
                    CanViewAllTickets = model.CanViewAllTickets,
                    CanManageKnowledgeBase = model.CanManageKnowledgeBase,
                    CanViewReports = model.CanViewReports,
                    CanManageTeams = model.CanManageTeams,
                    IsSystemRole = false // Custom roles are not system roles
                };

                _context.Roles.Add(newRole);
                await _context.SaveChangesAsync();

                // Log the action
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var auditLog = new AuditLog
                {
                    UserID = currentUserId,
                    Action = $"Created new role: {model.RoleName}",
                    PerformedAt = DateTime.UtcNow
                };
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Role '{model.RoleName}' has been created successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while creating the role.";
                // Log the exception
                Console.WriteLine($"Error creating role: {ex.Message}");
                return View(model);
            }
        }

        // GET: UserManagement/EditRole/5
        public async Task<IActionResult> EditRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                TempData["ErrorMessage"] = "Role not found.";
                return RedirectToAction("Index");
            }

            var model = new CreateRoleViewModel
            {
                RoleID = role.RoleID,
                RoleName = role.RoleName,
                Description = role.Description ?? "",
                CanApproveUsers = role.CanApproveUsers,
                CanManageTickets = role.CanManageTickets,
                CanViewAllTickets = role.CanViewAllTickets,
                CanManageKnowledgeBase = role.CanManageKnowledgeBase,
                CanViewReports = role.CanViewReports,
                CanManageTeams = role.CanManageTeams,
                IsSystemRole = role.IsSystemRole
            };

            return View("CreateRole", model);
        }

        // POST: UserManagement/EditRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(CreateRoleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("CreateRole", model);
            }

            try
            {
                var role = await _context.Roles.FindAsync(model.RoleID);
                if (role == null)
                {
                    TempData["ErrorMessage"] = "Role not found.";
                    return RedirectToAction("Index");
                }

                // Check if role name already exists (excluding current role)
                var existingRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.RoleName.ToLower() == model.RoleName.ToLower()
                                            && r.RoleID != model.RoleID);

                if (existingRole != null)
                {
                    ModelState.AddModelError("RoleName", "A role with this name already exists.");
                    return View("CreateRole", model);
                }

                role.RoleName = model.RoleName;
                role.Description = model.Description;
                role.CanApproveUsers = model.CanApproveUsers;
                role.CanManageTickets = model.CanManageTickets;
                role.CanViewAllTickets = model.CanViewAllTickets;
                role.CanManageKnowledgeBase = model.CanManageKnowledgeBase;
                role.CanViewReports = model.CanViewReports;
                role.CanManageTeams = model.CanManageTeams;

                await _context.SaveChangesAsync();

                // Log the action
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var auditLog = new AuditLog
                {
                    UserID = currentUserId,
                    Action = $"Updated role: {model.RoleName}",
                    PerformedAt = DateTime.UtcNow
                };
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Role '{model.RoleName}' has been updated successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while updating the role.";
                Console.WriteLine($"Error updating role: {ex.Message}");
                return View("CreateRole", model);
            }
        }

        // GET: UserManagement/RoleManagement
        public async Task<IActionResult> RoleManagement()
        {
            var roles = await _context.Roles
                .OrderBy(r => r.RoleName)
                .ToListAsync();

            return View(roles);
        }

        // POST: UserManagement/DeleteRole/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRole(int id)
        {
            try
            {
                var role = await _context.Roles.FindAsync(id);
                if (role == null)
                {
                    TempData["ErrorMessage"] = "Role not found.";
                    return RedirectToAction("RoleManagement");
                }

                if (role.IsSystemRole)
                {
                    TempData["ErrorMessage"] = "System roles cannot be deleted.";
                    return RedirectToAction("RoleManagement");
                }

                // Check if any users are assigned to this role
                var usersWithRole = await _context.Users.CountAsync(u => u.RoleID == id);
                if (usersWithRole > 0)
                {
                    TempData["ErrorMessage"] = $"Cannot delete role '{role.RoleName}' because it is assigned to {usersWithRole} user(s).";
                    return RedirectToAction("RoleManagement");
                }

                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();

                // Log the action
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var auditLog = new AuditLog
                {
                    UserID = currentUserId,
                    Action = $"Deleted role: {role.RoleName}",
                    PerformedAt = DateTime.UtcNow,
                };
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Role '{role.RoleName}' has been deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the role.";
                Console.WriteLine($"Error deleting role: {ex.Message}");
            }

            return RedirectToAction("RoleManagement");
        }
    }
}