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

            var agents = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName == "Support Agent")
                .ToListAsync();

            var availableAgents = agents
                .Where(agent =>
                    _context.Tickets
                        .Count(t => t.AssignedTo == agent.UserID && t.Status.StatusName != "Closed") < 5)
                .ToList();

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