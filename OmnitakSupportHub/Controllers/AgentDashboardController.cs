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

        public async Task<IActionResult> Index()
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

            // Get assigned tickets with full details
            var assignedTickets = await _context.Tickets
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.Category)
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .Where(t => t.AssignedTo == agent.UserID)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // Get chat messages for assigned tickets
            var ticketIds = assignedTickets.Select(t => t.TicketID).ToList();
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

            // Get dashboard statistics
            var totalTicketsCount = await _context.Tickets.CountAsync();
            var assignedToMeCount = assignedTickets.Count;
            var inProgressCount = assignedTickets.Count(t => t.Status?.StatusName == "In Progress");
            var resolvedTodayCount = assignedTickets.Count(t =>
                t.Status?.StatusName == "Resolved" &&
                t.ClosedAt?.Date == DateTime.Today);

            var viewModel = new AgentDashboardViewModel
            {
                AgentName = agent.FullName,
                TeamName = agent.Team?.TeamName ?? "No Team Assigned",
                DepartmentName = agent.Department?.DepartmentName ?? "No Department",
                AssignedTickets = assignedTickets,
                TicketChats = ticketChats,
                TotalTickets = totalTicketsCount,
                AssignedToMe = assignedToMeCount,
                InProgress = inProgressCount,
                ResolvedToday = resolvedTodayCount,
                AgentRole = agent.Role?.RoleName ?? "Support Agent"
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
                if (newStatus == "Resolved")
                {
                    ticket.ClosedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Add audit log
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                var agent = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (agent != null)
                {
                    var auditLog = new AuditLog
                    {
                        UserID = agent.UserID,
                        Action = "UPDATE_TICKET_STATUS",
                        TargetType = "Ticket",
                        TargetID = ticketId,
                        Details = $"Status changed to {newStatus}",
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
                // Log the exception here if you have logging configured
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
                // Log the exception here if you have logging configured
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

            ViewBag.ChatMessages = chatMessages;
            return View(ticket);
        }

        public async Task<IActionResult> GetTicketStats()
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var agent = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (agent == null)
            {
                return Json(new { error = "Agent not found" });
            }

            var stats = new
            {
                totalTickets = await _context.Tickets.CountAsync(),
                assignedToMe = await _context.Tickets.CountAsync(t => t.AssignedTo == agent.UserID),
                inProgress = await _context.Tickets.CountAsync(t =>
                    t.AssignedTo == agent.UserID && t.Status.StatusName == "In Progress"),
                resolvedToday = await _context.Tickets.CountAsync(t =>
                    t.AssignedTo == agent.UserID &&
                    t.Status.StatusName == "Resolved" &&
                    t.ClosedAt.HasValue && t.ClosedAt.Value.Date == DateTime.Today)
            };

            return Json(stats);
        }

        [HttpGet]
        public async Task<IActionResult> SearchTickets(string query)
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var agent = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (agent == null || string.IsNullOrWhiteSpace(query))
            {
                return Json(new List<object>());
            }

            var tickets = await _context.Tickets
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.Category)
                .Include(t => t.CreatedByUser)
                .Where(t => t.AssignedTo == agent.UserID &&
                    (t.Title.Contains(query) ||
                     t.Description.Contains(query) ||
                     t.TicketID.ToString().Contains(query) ||
                     t.CreatedByUser.FullName.Contains(query)))
                .Select(t => new
                {
                    id = t.TicketID,
                    title = t.Title,
                    status = t.Status.StatusName,
                    priority = t.Priority.PriorityName,
                    createdBy = t.CreatedByUser.FullName,
                    createdAt = t.CreatedAt.ToString("MMM dd, yyyy")
                })
                .Take(10)
                .ToListAsync();

            return Json(tickets);
        }
    }
}