using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;
using OmnitakSupportHub.Models.ViewModels;

namespace OmnitakSupportHub.Controllers
{
    [Authorize]
    public class TicketController : Controller
    {
        private readonly OmnitakContext _context;

        public TicketController(OmnitakContext context)
        {
            _context = context;
        }

        // GET: /Ticket
        public async Task<IActionResult> Index()
        {
            var tickets = await _context.Tickets
                .Include(t => t.Category)
                .Include(t => t.Priority)
                .Include(t => t.Status)
                .Include(t => t.AssignedToUser)
                .ToListAsync();

            return View(tickets);
        }

        // GET: /Ticket/Assign/5
        public async Task<IActionResult> Assign(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(t => t.TicketID == id);

            if (ticket == null)
                return NotFound();

            var agents = await _context.Users
                .Where(u => u.IsActive && u.Role!.RoleName == "Support Agent")
                .Select(u => new SelectListItem
                {
                    Value = u.UserID.ToString(),
                    Text = u.FullName
                }).ToListAsync();

            var viewModel = new AssignTicketViewModel
            {
                TicketID = ticket.TicketID,
                CurrentAssignee = ticket.AssignedToUser?.FullName,
                AvailableAgents = agents
            };

            return View(viewModel);
        }

        // POST: /Ticket/Assign/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AssignTicketViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var ticket = await _context.Tickets.FindAsync(model.TicketID);
            if (ticket == null)
                return NotFound();

            ticket.AssignedTo = model.SelectedAgentID;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Ticket successfully assigned!";
            return RedirectToAction(nameof(Index));
        }
        // GET: /Ticket/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Category)
                .Include(t => t.Priority)
                .Include(t => t.Status)
                .FirstOrDefaultAsync(m => m.TicketID == id);

            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        // POST: /Ticket/Delete/5
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket != null)
            {
                _context.Tickets.Remove(ticket);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
        // GET: /Ticket/TestError
        [HttpGet("/Ticket/TestError")]
        [AllowAnonymous] // Optional: test without login
        public IActionResult TestError()
        {
            throw new Exception("Middleware test: something went wrong!");
        }

        // User Ticket Actions
        // User Ticket Creation
        [HttpGet]
        [Authorize(Roles = "End User")]
        public async Task<IActionResult> Create()
        {
            var model = new CreateTicketViewModel
            {
                Categories = await _context.Categories
                    .Select(c => new SelectListItem { Value = c.CategoryID.ToString(), Text = c.CategoryName })
                    .ToListAsync(),
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "End User")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTicketViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = await _context.Categories
                    .Select(c => new SelectListItem { Value = c.CategoryID.ToString(), Text = c.CategoryName }).ToListAsync();
            }

            int userId = int.Parse(User.FindFirst("UserID")?.Value ?? "0");

            var ticket = new Ticket
            {
                Title = model.Title,
                Description = model.Description,
                CategoryID = model.CategoryId,
                PriorityID = await _context.Priorities
                .Where(p => p.PriorityName == "Low") // or default name like "Low"
                .Select(p => p.PriorityID)
                .FirstOrDefaultAsync(),

                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                StatusID = await _context.Statuses
                    .Where(s => s.StatusName == "Open")
                    .Select(s => s.StatusID)
                    .FirstOrDefaultAsync()
            };

            // Apply routing rule (optional)
            var rule = await _context.RoutingRules
                .FirstOrDefaultAsync(r =>
                    r.CategoryID == model.CategoryId && r.IsActive);


            if (rule != null)
            {
                ticket.TeamID = rule.TeamID;

                var agentId = await _context.Users
                    .Where(u => u.TeamID == rule.TeamID && u.Role.RoleName == "Support Agent")
                    .Select(u => u.UserID)
                    .FirstOrDefaultAsync();

                if (agentId != 0)
                {
                    ticket.AssignedTo = agentId;
                    ticket.StatusID = await _context.Statuses
                        .Where(s => s.StatusName == "Assigned")
                        .Select(s => s.StatusID)
                        .FirstOrDefaultAsync();
                }
            }

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            _context.TicketTimelines.Add(new TicketTimeline
            {
                TicketID = ticket.TicketID,
                ChangedByUserID = userId,
                ChangeTime = DateTime.UtcNow,
                OldStatus = "N/A",
                NewStatus = rule != null ? "Assigned" : "Open"
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Ticket submitted successfully!";
            return RedirectToAction("Index", "UserDashboard");
        }


        // Manager Dashboard Actions
        [HttpGet]
        public async Task<IActionResult> ManagerDashboard(DateTime? startDate, DateTime? endDate)
        {
            if (!User.IsInRole("Support Manager"))
                return Forbid();

            // Default: show last 30 days if no filter
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            var openTickets = await _context.Tickets
                .Include(t => t.Category)
                    .ThenInclude(c => c.RoutingRules)
                        .ThenInclude(r => r.Team)
                .Include(t => t.Priority)
                .Include(t => t.Status)
                .Where(t => t.Status.StatusName == "Open" && t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .ToListAsync();

            // Group tickets by Category
            var groupedTickets = openTickets
                .GroupBy(t => t.Category)
                .ToList();

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(groupedTickets);
        }

        // Ticket Details for Support Manager
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (!User.IsInRole("Support Manager"))
                return Forbid();

            var ticket = await _context.Tickets
                .Include(t => t.Category)
                    .ThenInclude(c => c.RoutingRules)
                        .ThenInclude(r => r.Team)
                            .ThenInclude(st => st.Users)
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(t => t.TicketID == id);

            if (ticket == null) return NotFound();

            // Find the SupportTeam for this Category
            var supportTeam = ticket.Category.RoutingRules.FirstOrDefault()?.Team;

            // Get available agents in that team
            var availableAgents = supportTeam?.Users
                .Where(u => u.IsActive && _context.Tickets.Count(t => t.AssignedTo == u.UserID && t.Status.StatusName != "Closed") < 5)
                .ToList();

            ViewBag.AvailableAgents = availableAgents;
            ViewBag.Priorities = new SelectList(_context.Priorities, "PriorityID", "PriorityName");

            return View(ticket);
        }

        // Assign Agent to Ticket (Support Manager)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAgent(int ticketId, int agentId)
        {
            if (!User.IsInRole("Support Manager"))
                return Forbid();

            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) return NotFound();

            ticket.AssignedTo = agentId;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Ticket assigned successfully!";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        // Set Ticket Priority (Support Manager)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPriority(int ticketId, int priorityId)
        {
            if (!User.IsInRole("Support Manager"))
                return Forbid();

            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) return NotFound();

            ticket.PriorityID = priorityId;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Priority updated successfully!";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        // Close Ticket (Support Manager)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseTicket(int ticketId)
        {
            if (!User.IsInRole("Support Manager"))
                return Forbid();

            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) return NotFound();

            var closedStatus = await _context.Statuses.FirstOrDefaultAsync(s => s.StatusName == "Closed");
            if (closedStatus == null) return BadRequest("Closed status not found.");

            ticket.StatusID = closedStatus.StatusID;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Ticket closed!";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

    }
}
