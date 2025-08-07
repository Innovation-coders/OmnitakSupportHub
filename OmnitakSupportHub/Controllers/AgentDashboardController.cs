using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;
using OmnitakSupportHub.Models.ViewModels;
using System.Security.Claims;

namespace OmnitakSupportHub.Controllers
{
    [Authorize(Roles = "Support Agent,Administrator")]
    public class AgentDashboardController : Controller
    {
        private readonly OmnitakContext _context;

        public AgentDashboardController(OmnitakContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string statusFilter = "", string priorityFilter = "", string searchTerm = "")
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var agent = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Team)
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (agent == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get assigned tickets with enhanced filtering
            var ticketsQuery = _context.Tickets
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.Category)
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .Include(t => t.TicketTimelines)
                .Where(t => t.AssignedTo == agent.UserID);

            // Apply filters
            if (!string.IsNullOrEmpty(statusFilter))
            {
                ticketsQuery = ticketsQuery.Where(t => t.Status.StatusName == statusFilter);
            }

            if (!string.IsNullOrEmpty(priorityFilter))
            {
                ticketsQuery = ticketsQuery.Where(t => t.Priority.PriorityName == priorityFilter);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                ticketsQuery = ticketsQuery.Where(t =>
                    t.Title.Contains(searchTerm) ||
                    t.Description.Contains(searchTerm) ||
                    t.CreatedByUser.FullName.Contains(searchTerm));
            }

            var assignedTickets = await ticketsQuery
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // Get recent activity/timeline for tickets
            var ticketIds = assignedTickets.Select(t => t.TicketID).ToList();
            var recentActivity = await _context.TicketTimelines
                .Where(tt => ticketIds.Contains(tt.TicketID))
                .OrderByDescending(tt => tt.ChangeTime)
                .Take(10)
                .ToListAsync();

            // Get chat messages for assigned tickets
            var chatMessages = await _context.ChatMessages
                .Include(c => c.User)
                .Include(c => c.Ticket)
                .Where(c => ticketIds.Contains(c.TicketID))
                .OrderBy(c => c.SentAt)
                .ToListAsync();

            // Group chat messages by ticket
            var ticketChats = chatMessages
                .GroupBy(c => c.TicketID)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Calculate enhanced statistics
            var totalTicketsCount = await _context.Tickets.CountAsync();
            var assignedToMeCount = assignedTickets.Count;
            var inProgressCount = assignedTickets.Count(t => t.Status?.StatusName == "In Progress");
            var resolvedTodayCount = assignedTickets.Count(t =>
                t.Status?.StatusName == "Resolved" &&
                t.ClosedAt?.Date == DateTime.Today);
            var newTicketsCount = assignedTickets.Count(t => t.Status?.StatusName == "New");
            var overdueTicketsCount = GetOverdueTicketsCount(assignedTickets);
            var highPriorityCount = assignedTickets.Count(t => t.Priority?.PriorityName == "High");
            var pendingUserCount = assignedTickets.Count(t => t.Status?.StatusName == "Pending User");

            // Calculate performance metrics
            var resolvedTicketsThisWeek = assignedTickets.Count(t =>
                t.Status?.StatusName == "Resolved" &&
                t.ClosedAt >= DateTime.Today.AddDays(-7));

            var resolvedTicketsThisMonth = assignedTickets.Count(t =>
                t.Status?.StatusName == "Resolved" &&
                t.ClosedAt >= DateTime.Today.AddDays(-30));

            var averageResolutionTime = CalculateAverageResolutionTime(assignedTickets);
            var resolutionRate = assignedTickets.Count > 0 ?
                (double)assignedTickets.Count(t => t.Status?.StatusName == "Resolved") / assignedTickets.Count * 100 : 0;

            // Get available statuses and priorities for filters
            var availableStatuses = await _context.Statuses
                .Where(s => s.IsActive)
                .Select(s => s.StatusName)
                .ToListAsync();

            var availablePriorities = await _context.Priorities
                .Where(p => p.IsActive)
                .Select(p => p.PriorityName)
                .ToListAsync();

            var viewModel = new AgentDashboardViewModel
            {
                AgentName = agent.FullName,
                TeamName = agent.Team?.TeamName ?? "No Team Assigned",
                DepartmentName = agent.Department?.DepartmentName ?? "No Department",
                AssignedTickets = assignedTickets,
                TicketChats = ticketChats,
                RecentActivity = recentActivity,
                TotalTickets = totalTicketsCount,
                AssignedToMe = assignedToMeCount,
                InProgress = inProgressCount,
                ResolvedToday = resolvedTodayCount,
                NewTickets = newTicketsCount,
                OverdueTickets = overdueTicketsCount,
                HighPriorityTickets = highPriorityCount,
                PendingUserTickets = pendingUserCount,
                AgentRole = agent.Role?.RoleName ?? "Support Agent",
                AverageResponseTime = averageResolutionTime,
                ResolutionRate = resolutionRate,
                TicketsResolvedThisWeek = resolvedTicketsThisWeek,
                TicketsResolvedThisMonth = resolvedTicketsThisMonth,

                // Filter data
                CurrentStatusFilter = statusFilter,
                CurrentPriorityFilter = priorityFilter,
                CurrentSearchTerm = searchTerm,
                AvailableStatuses = availableStatuses,
                AvailablePriorities = availablePriorities
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int ticketId, string newStatus)
        {
            try
            {
                var ticket = await _context.Tickets
                    .Include(t => t.Status)
                    .FirstOrDefaultAsync(t => t.TicketID == ticketId);

                if (ticket == null)
                {
                    TempData["ErrorMessage"] = "Ticket not found.";
                    return RedirectToAction("Index");
                }

                var oldStatus = ticket.Status?.StatusName ?? "Unknown";

                // Get the status entity
                var status = await _context.Statuses
                    .FirstOrDefaultAsync(s => s.StatusName == newStatus);

                if (status == null)
                {
                    TempData["ErrorMessage"] = "Invalid status.";
                    return RedirectToAction("Index");
                }

                // Update ticket status
                ticket.StatusID = status.StatusID;

                // If marking as resolved, set the closed date
                if (newStatus == "Resolved" || newStatus == "Closed")
                {
                    ticket.ClosedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Add to timeline
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                var agent = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (agent != null)
                {
                    var timeline = new TicketTimeline
                    {
                        TicketID = ticketId,
                        ChangedByUserID = agent.UserID,
                        ChangeTime = DateTime.UtcNow,
                        OldStatus = oldStatus,
                        NewStatus = newStatus
                    };
                    _context.TicketTimelines.Add(timeline);

                    // Add audit log
                    var auditLog = new AuditLog
                    {
                        UserID = agent.UserID,
                        Action = "UPDATE_TICKET_STATUS",
                        TargetType = "Ticket",
                        TargetID = ticketId,
                        Details = $"Status changed from {oldStatus} to {newStatus}",
                        PerformedAt = DateTime.UtcNow
                    };
                    _context.AuditLogs.Add(auditLog);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = $"Ticket #{ticketId} status updated to {newStatus} successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while updating the ticket status.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int ticketId, string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    TempData["ErrorMessage"] = "Comment cannot be empty.";
                    return RedirectToAction("Index");
                }

                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                var agent = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (agent == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var chatMessage = new ChatMessage
                {
                    TicketID = ticketId,
                    UserID = agent.UserID,
                    Message = message.Trim(),
                    SentAt = DateTime.UtcNow
                };

                _context.ChatMessages.Add(chatMessage);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Comment added successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while adding the comment.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePriority(int ticketId, string priority)
        {
            try
            {
                var ticket = await _context.Tickets.FindAsync(ticketId);
                if (ticket == null)
                {
                    TempData["ErrorMessage"] = "Ticket not found.";
                    return RedirectToAction("Index");
                }

                var priorityEntity = await _context.Priorities
                    .FirstOrDefaultAsync(p => p.PriorityName == priority);

                if (priorityEntity == null)
                {
                    TempData["ErrorMessage"] = "Invalid priority.";
                    return RedirectToAction("Index");
                }

                ticket.PriorityID = priorityEntity.PriorityID;
                await _context.SaveChangesAsync();

                // Add timeline entry
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                var agent = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (agent != null)
                {
                    var timeline = new TicketTimeline
                    {
                        TicketID = ticketId,
                        ChangedByUserID = agent.UserID,
                        ChangeTime = DateTime.UtcNow,
                        OldStatus = "Priority Updated",
                        NewStatus = $"Priority set to {priority}"
                    };
                    _context.TicketTimelines.Add(timeline);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = $"Priority updated to {priority} successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while updating the priority.";
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> TicketDetails(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.Category)
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .Include(t => t.TicketTimelines)
                .FirstOrDefaultAsync(t => t.TicketID == id);

            if (ticket == null)
            {
                return NotFound();
            }

            // Get chat messages for this ticket
            var chatMessages = await _context.ChatMessages
                .Include(c => c.User)
                .Where(c => c.TicketID == id)
                .OrderBy(c => c.SentAt)
                .ToListAsync();

            // Get timeline for this ticket
            var timeline = await _context.TicketTimelines
                .Where(t => t.TicketID == id)
                .OrderByDescending(t => t.ChangeTime)
                .ToListAsync();

            ViewBag.ChatMessages = chatMessages;
            ViewBag.Timeline = timeline;
            ViewBag.AvailableStatuses = await _context.Statuses.Where(s => s.IsActive).ToListAsync();
            ViewBag.AvailablePriorities = await _context.Priorities.Where(p => p.IsActive).ToListAsync();

            return View(ticket);
        }

        // API endpoints for real-time updates
        [HttpGet]
        public async Task<IActionResult> GetTicketStats()
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var agent = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (agent == null)
            {
                return Json(new { error = "Agent not found" });
            }

            var assignedTickets = await _context.Tickets
                .Include(t => t.Status)
                .Where(t => t.AssignedTo == agent.UserID)
                .ToListAsync();

            var stats = new
            {
                totalTickets = assignedTickets.Count,
                newTickets = assignedTickets.Count(t => t.Status.StatusName == "New"),
                inProgress = assignedTickets.Count(t => t.Status.StatusName == "In Progress"),
                resolvedToday = assignedTickets.Count(t =>
                    t.Status.StatusName == "Resolved" &&
                    t.ClosedAt.HasValue && t.ClosedAt.Value.Date == DateTime.Today),
                overdue = GetOverdueTicketsCount(assignedTickets),
                highPriority = assignedTickets.Count(t => t.Priority?.PriorityName == "High")
            };

            return Json(stats);
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentActivity()
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var agent = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (agent == null)
            {
                return Json(new { error = "Agent not found" });
            }

            var ticketIds = await _context.Tickets
                .Where(t => t.AssignedTo == agent.UserID)
                .Select(t => t.TicketID)
                .ToListAsync();

            var recentActivity = await _context.TicketTimelines
                .Include(t => t.Ticket)
                .Where(t => ticketIds.Contains(t.TicketID))
                .OrderByDescending(t => t.ChangeTime)
                .Take(5)
                .Select(t => new
                {
                    ticketId = t.TicketID,
                    ticketTitle = t.Ticket.Title,
                    oldStatus = t.OldStatus,
                    newStatus = t.NewStatus,
                    changeTime = t.ChangeTime.ToString("MMM dd, HH:mm")
                })
                .ToListAsync();

            return Json(recentActivity);
        }

        // Helper methods
        private int GetOverdueTicketsCount(List<Ticket> tickets)
        {
            var now = DateTime.UtcNow;
            return tickets.Count(t =>
                t.Status?.StatusName != "Resolved" &&
                t.Status?.StatusName != "Closed" &&
                t.CreatedAt.AddDays(2) < now); // Consider 2+ days as overdue
        }

        private double CalculateAverageResolutionTime(List<Ticket> tickets)
        {
            var resolvedTickets = tickets.Where(t =>
                t.Status?.StatusName == "Resolved" && t.ClosedAt.HasValue).ToList();

            if (!resolvedTickets.Any())
                return 0;

            var totalHours = resolvedTickets.Sum(t =>
                (t.ClosedAt.Value - t.CreatedAt).TotalHours);

            return Math.Round(totalHours / resolvedTickets.Count, 1);
        }
    }
}