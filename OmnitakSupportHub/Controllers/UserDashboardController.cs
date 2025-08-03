using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;
using OmnitakSupportHub.Models.ViewModels;
using System.Security.Claims;

namespace OmnitakSupportHub.Controllers
{
    [Authorize(Roles = "End User")]
    public class UserDashboardController : Controller
    {
        private readonly OmnitakContext _context;

        public UserDashboardController(OmnitakContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            int userId = int.Parse(User.FindFirst("UserID")?.Value ?? "0");

            var myTickets = await _context.Tickets
                .Include(t => t.Category)
                .Include(t => t.Priority)
                .Include(t => t.Status)
                .Where(t => t.CreatedBy == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(myTickets);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            int userId = int.Parse(User.FindFirst("UserID")?.Value ?? "0");
            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
            {
                return NotFound();
            }

            var model = new ProfileViewModel
            {
                FirstName = user.FullName?.Split(' ').FirstOrDefault(),
                LastName = user.FullName?.Split(' ').LastOrDefault(),
                Email = user.Email,
                Phone = "", // Add phone to Users table if needed
                Department = user.Department?.DepartmentName ?? "Marketing", // Default for demo
                JobTitle = "", // Add job title to Users table if needed
                AvatarInitials = GetInitials(user.FullName),
                MemberSince = user.CreatedAt.ToString("MMM yyyy")
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                int userId = int.Parse(User.FindFirst("UserID")?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);

                if (user != null)
                {
                    user.FullName = $"{model.FirstName} {model.LastName}";
                    user.Email = model.Email;
                    // Add phone and job title fields to Users table if needed

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Your profile has been updated successfully.";
                    return RedirectToAction(nameof(Profile));
                }
            }

            // If we got this far, something failed, redisplay form
            model.AvatarInitials = GetInitials($"{model.FirstName} {model.LastName}");
            return View(model);
        }

        private string GetInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "US";

            var names = fullName.Split(' ');
            if (names.Length >= 2)
                return $"{names[0][0]}{names[1][0]}";

            return fullName.Length >= 2
                ? fullName.Substring(0, 2).ToUpper()
                : fullName.ToUpper();
        }
    }
}