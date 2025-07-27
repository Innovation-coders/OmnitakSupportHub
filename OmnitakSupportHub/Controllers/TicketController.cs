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

        // Manager Create Ticket
        [HttpGet]
        [Authorize(Roles = "Support Manager")]
        public async Task<IActionResult> ManagerCreate()
        {
            var model = new CreateTicketViewModel
            {
                Categories = await _context.Categories
                    .Select(c => new SelectListItem { Value = c.CategoryID.ToString(), Text = c.CategoryName })
                    .ToListAsync(),

                Priorities = await _context.Priorities
                    .Select(p => new SelectListItem { Value = p.PriorityID.ToString(), Text = p.PriorityName })
                    .ToListAsync(),

                Statuses = await _context.Statuses
                    .Select(s => new SelectListItem { Value = s.StatusID.ToString(), Text = s.StatusName })
                    .ToListAsync(),

                Agents = await _context.Users
                    .Where(u => u.IsActive && u.Role.RoleName == "Support Agent")
                    .Select(u => new SelectListItem { Value = u.UserID.ToString(), Text = u.FullName })
                    .ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Support Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManagerCreate(CreateTicketViewModel model, int? assignedTo, int? priorityId, int? statusId)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = await _context.Categories
                    .Select(c => new SelectListItem { Value = c.CategoryID.ToString(), Text = c.CategoryName })
                    .ToListAsync();
                ViewBag.Priorities = new SelectList(_context.Priorities, "PriorityID", "PriorityName");
                ViewBag.Statuses = new SelectList(_context.Statuses, "StatusID", "StatusName");

                var agents = await _context.Users
                    .Include(u => u.Role)
                    .Where(u => u.IsActive && u.Role.RoleName == "Support Agent")
                    .ToListAsync();
                ViewBag.Agents = new SelectList(agents, "UserID", "FullName");

                return View(model);
            }

            int userId = int.Parse(User.FindFirst("UserID")?.Value ?? "0");

            var ticket = new Ticket
            {
                Title = model.Title,
                Description = model.Description,
                CategoryID = model.CategoryId,
                PriorityID = priorityId ?? await _context.Priorities
                    .Where(p => p.PriorityName == "Medium")
                    .Select(p => p.PriorityID)
                    .FirstOrDefaultAsync(),
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                StatusID = statusId ?? await _context.Statuses
                    .Where(s => s.StatusName == "Open")
                    .Select(s => s.StatusID)
                    .FirstOrDefaultAsync(),
                AssignedTo = assignedTo
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Ticket created successfully!";
            return RedirectToAction("Index", "ManagerDashboard");
        }

        // Manager Edit Ticket
        [HttpGet]
        [Authorize(Roles = "Support Manager")]
        public async Task<IActionResult> Edit(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Category)
                .Include(t => t.Priority)
                .Include(t => t.Status)
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(t => t.TicketID == id);

            if (ticket == null) return NotFound();

            var model = new CreateTicketViewModel
            {
                Title = ticket.Title,
                Description = ticket.Description,
                CategoryId = ticket.CategoryID,
                Categories = await _context.Categories
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryID.ToString(),
                        Text = c.CategoryName,
                        Selected = c.CategoryID == ticket.CategoryID
                    })
                    .ToListAsync()
            };

            ViewBag.TicketId = ticket.TicketID;
            ViewBag.Priorities = new SelectList(_context.Priorities, "PriorityID", "PriorityName", ticket.PriorityID);
            ViewBag.Statuses = new SelectList(_context.Statuses, "StatusID", "StatusName", ticket.StatusID);

            var agents = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.IsActive && u.Role.RoleName == "Support Agent")
                .ToListAsync();
            ViewBag.Agents = new SelectList(agents, "UserID", "FullName", ticket.AssignedTo);

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Support Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateTicketViewModel model, int? assignedTo, int? priorityId, int? statusId)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            if (!ModelState.IsValid)
            {
                model.Categories = await _context.Categories
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryID.ToString(),
                        Text = c.CategoryName,
                        Selected = c.CategoryID == model.CategoryId
                    })
                    .ToListAsync();

                ViewBag.TicketId = id;
                ViewBag.Priorities = new SelectList(_context.Priorities, "PriorityID", "PriorityName", priorityId);
                ViewBag.Statuses = new SelectList(_context.Statuses, "StatusID", "StatusName", statusId);

                var agents = await _context.Users
                    .Include(u => u.Role)
                    .Where(u => u.IsActive && u.Role.RoleName == "Support Agent")
                    .ToListAsync();
                ViewBag.Agents = new SelectList(agents, "UserID", "FullName", assignedTo);

                return View(model);
            }

            ticket.Title = model.Title;
            ticket.Description = model.Description;
            ticket.CategoryID = model.CategoryId;
            ticket.PriorityID = priorityId ?? ticket.PriorityID;
            ticket.StatusID = statusId ?? ticket.StatusID;
            ticket.AssignedTo = assignedTo;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Ticket updated successfully!";
            return RedirectToAction("Details", new { id = id });
        }

        // Ticket Details for Support Manager
        [HttpGet]
        [Authorize(Roles = "Support Manager")]
        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Category)
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Team)
                .FirstOrDefaultAsync(t => t.TicketID == id);

            if (ticket == null) return NotFound();

            // Get all available agents (not just from specific team)
            var allAgents = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.IsActive && u.Role.RoleName == "Support Agent")
                .ToListAsync();

            // Filter agents with less than 5 active tickets
            var availableAgents = await _context.Users
                .Where(u => u.IsActive && u.Role.RoleName == "Support Agent")
                .Select(u => new
                {
                    Agent = u,
                    ActiveTicketCount = u.AssignedTickets
                        .Count(t => t.Status.StatusName != "Closed" && t.Status.StatusName != "Resolved")
                })
                .Where(x => x.ActiveTicketCount < 5)
                .Select(x => x.Agent)
                .ToListAsync();

            ViewBag.AvailableAgents = availableAgents;
            ViewBag.Priorities = new SelectList(_context.Priorities, "PriorityID", "PriorityName", ticket.PriorityID);
            ViewBag.CurrentAssignedAgent = ticket.AssignedTo;

            return View(ticket);
        }

        // Assign Agent to Ticket (Support Manager)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Support Manager")]
        public async Task<IActionResult> AssignAgent(int ticketId, int agentId)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Status)
                .FirstOrDefaultAsync(t => t.TicketID == ticketId);

            if (ticket == null) return NotFound();

            // Verify the agent exists and is active
            var agent = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserID == agentId && u.IsActive && u.Role.RoleName == "Support Agent");

            if (agent == null)
            {
                TempData["ErrorMessage"] = "Selected agent is not valid or not active.";
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            var oldAssignedTo = ticket.AssignedTo;
            ticket.AssignedTo = agentId;

            // Update status to "Assigned" if it's currently "Open"
            if (ticket.Status.StatusName == "Open")
            {
                var assignedStatus = await _context.Statuses
                    .FirstOrDefaultAsync(s => s.StatusName == "Assigned");
                if (assignedStatus != null)
                {
                    ticket.StatusID = assignedStatus.StatusID;
                }
            }

            await _context.SaveChangesAsync();

            // Log the assignment change
            var userId = int.Parse(User.FindFirst("UserID")?.Value ?? "0");
            _context.TicketTimelines.Add(new TicketTimeline
            {
                TicketID = ticketId,
                ChangedByUserID = userId,
                ChangeTime = DateTime.UtcNow,
                OldStatus = oldAssignedTo?.ToString() ?? "Unassigned",
                NewStatus = $"Assigned to {agent.FullName}"
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Ticket successfully assigned to {agent.FullName}!";
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
