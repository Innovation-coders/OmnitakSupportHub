using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models.ViewModels;
using OmnitakSupportHub.Models;
using OmnitakSupportHub.Services;
using System.Security.Claims;

namespace OmnitakSupportHub.Controllers
{
    [Authorize(Roles = "Support Agent")]
    public class AgentDashboardController : Controller
    {
        private readonly OmnitakContext _context; // Adjust this to your DbContext name

        public AgentDashboardController(OmnitakContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get the current user's ID
            var UserID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get the current user with their details
            var currentUser = await _context.Users
                .Include(u => u.Team) // Assuming you have a Team navigation property
                .Include(u => u.AssignedTickets) // Include assigned tickets
                .FirstOrDefaultAsync(u => u.UserID == int.Parse(UserID));

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get tickets assigned to this agent
            var assignedTickets = currentUser.AssignedTickets;

            // Get chat messages for each ticket
            var ticketIds = assignedTickets.Select(t => t.TicketID).ToList();
            var chatMessages = await _context.ChatMessages
                .Include(c => c.User)
                .Where(c => ticketIds.Contains(c.TicketID)) // Adjust property name as needed
                .ToListAsync();

            // Group chat messages by ticket ID
            var ticketChats = chatMessages
                .GroupBy(c => c.TicketID)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Create the view model
            var viewModel = new AgentDashboardViewModel
            {
                AgentName = currentUser.FullName ?? currentUser.FullName,
                TeamName = currentUser.Team?.TeamName ?? "No Team Assigned", // Adjust as needed
                AssignedTickets = (List<Ticket>)currentUser.AssignedTickets,
                TicketChats = ticketChats
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int ticketId, string newStatus)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null)
            {
                TempData["ErrorMessage"] = "Ticket not found.";
                return RedirectToAction("Index");
            }

            // Get the status entity
            var status = await _context.Statuses // Adjust table name as needed
                .FirstOrDefaultAsync(s => s.StatusName == newStatus);

            if (status != null)
            {
                ticket.StatusID = status.StatusID; // Adjust property names as needed
                ticket.CreatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Ticket status updated to {newStatus}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid status selected.";
            }

            return RedirectToAction("Index");
        }
    }
}