using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;

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
    }
}