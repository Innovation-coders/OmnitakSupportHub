using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models.ViewModels;
using OmnitakSupportHub.Models;
using OmnitakSupportHub.Services;

namespace OmnitakSupportHub.Controllers
{
    [Authorize(Roles = "Support Manager")]
    public class ManagerDashboardController : Controller
    {
        private readonly OmnitakContext _context;
        private readonly IAuthService _authService;

        public ManagerDashboardController(OmnitakContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            // Default to last 30 days if no filter specified
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;


            var ticketsQuery = _context.Tickets
                .Include(t => t.Category)
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.CreatedByUser)
                .AsQueryable();

            if (startDate.HasValue)
            {
                ticketsQuery = ticketsQuery.Where(t => t.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                ticketsQuery = ticketsQuery.Where(t => t.CreatedAt <= endDate.Value);
            }

            var tickets = await ticketsQuery.ToListAsync();

            var groupedTickets = tickets.GroupBy(t => t.Category).ToList();

            // Get all support agents
            var allAgents = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.AssignedTickets.Where(t => t.Status.StatusName != "Closed" && t.Status.StatusName != "Resolved"))
                    .ThenInclude(t => t.Status)
                .Where(u => u.IsActive && u.Role.RoleName == "Support Agent")
                .ToListAsync();

            // Filter agents with less than 5 active tickets
            var availableAgents = new List<User>();
            foreach (var agent in allAgents)
            {
                var activeTicketCount = await _context.Tickets
                    .CountAsync(t => t.AssignedTo == agent.UserID &&
                               t.Status.StatusName != "Closed" &&
                               t.Status.StatusName != "Resolved");

                if (activeTicketCount < 5)
                {
                    // Set the count for display purposes
                    agent.AssignedTickets = await _context.Tickets
                        .Include(t => t.Status)
                        .Where(t => t.AssignedTo == agent.UserID &&
                                   t.Status.StatusName != "Closed" &&
                                   t.Status.StatusName != "Resolved")
                        .ToListAsync();

                    availableAgents.Add(agent);
                }
            }

            var viewModel = new ManagerDashboardViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                GroupedTickets = groupedTickets,
                AvailableAgents = availableAgents
            };

            return View(viewModel);
        }


       
    }
}