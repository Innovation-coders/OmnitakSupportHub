﻿using Microsoft.AspNetCore.Authorization;
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
            var agentTicketCounts = await _context.Users
                .Where(u => u.IsActive && u.Role.RoleName == "Support Agent")
                .Select(u => new
                {
                    Agent = u,
                    ActiveTicketCount = u.AssignedTickets
                        .Count(t => t.Status.StatusName != "Closed" && t.Status.StatusName != "Resolved")
                })
                .ToListAsync();

            var availableAgents = agentTicketCounts
                .Where(x => x.ActiveTicketCount < 5)
                .Select(x =>
                {
                    x.Agent.AssignedTickets = x.Agent.AssignedTickets
                        .Where(t => t.Status.StatusName != "Closed" && t.Status.StatusName != "Resolved")
                        .ToList();
                    return x.Agent;
                })
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