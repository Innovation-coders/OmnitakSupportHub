using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;
using OmnitakSupportHub.Models.ViewModels;
using System.Security.Claims;

namespace OmnitakSupportHub.Controllers
{
    [Authorize(Roles = "Support Agent")]
    public class AgentDashboardController : Controller
    {
        private readonly OmnitakContext _context;

        public AgentDashboardController(OmnitakContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string statusFilter = "", string priorityFilter = "", string searchTerm = "")
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                var agent = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Team)
                    .Include(u => u.Department)
                    .FirstOrDefaultAsync(u => u.Email == userEmail);

                if (agent == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var ticketsQuery = _context.Tickets
                    .Include(t => t.Status)
                    .Include(t => t.Priority)
                    .Include(t => t.Category)
                    .Include(t => t.CreatedByUser)
                    .Include(t => t.AssignedToUser)
                    .Include(t => t.TicketTimelines)
                    .Where(t => t.AssignedTo == agent.UserID);

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

                var ticketIds = assignedTickets.Select(t => t.TicketID).ToList();
                var recentActivity = await _context.TicketTimelines
                    .Where(tt => ticketIds.Contains(tt.TicketID))
                    .OrderByDescending(tt => tt.ChangeTime)
                    .Take(10)
                    .ToListAsync();

                var chatMessages = await _context.ChatMessages
                    .Include(c => c.User)
                    .Include(c => c.Ticket)
                    .Where(c => ticketIds.Contains(c.TicketID))
                    .OrderBy(c => c.SentAt)
                    .ToListAsync();

                var ticketChats = chatMessages
                    .GroupBy(c => c.TicketID)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var resolvedTodayCount = assignedTickets.Count(t =>
                    t.Status?.StatusName == "Resolved" &&
                    t.ClosedAt?.Date == DateTime.Today);

                var resolvedThisWeekCount = assignedTickets.Count(t =>
                    t.Status?.StatusName == "Resolved" &&
                    t.ClosedAt >= DateTime.Today.AddDays(-7));

                var averageResponseTime = CalculateAverageResponseTime(assignedTickets);

                var availableStatuses = await _context.Statuses
                    .Where(s => s.IsActive)
                    .Select(s => s.StatusName)
                    .ToListAsync();

                var availablePriorities = await _context.Priorities
                    .Where(p => p.IsActive)
                    .Select(p => p.PriorityName)
                    .ToListAsync();

               
                ViewBag.TotalKBArticles = await _context.KnowledgeBase.CountAsync();
                ViewBag.RecentKBArticles = await _context.KnowledgeBase
                    .Where(kb => kb.CreatedAt >= DateTime.Today.AddDays(-7))
                    .CountAsync();

                var viewModel = new AgentDashboardViewModel
                {
                    AgentName = agent.FullName,
                    TeamName = agent.Team?.TeamName ?? "No Team Assigned",
                    DepartmentName = agent.Department?.DepartmentName ?? "No Department Assigned",
                    AgentRole = agent.Role?.RoleName ?? "Support Agent",

                  
                    AssignedTickets = assignedTickets,
                    TicketChats = ticketChats,
                    RecentActivity = recentActivity,

                    AssignedToMe = assignedTickets.Count,
                    InProgress = assignedTickets.Count(t => t.Status?.StatusName == "In Progress"),
                    ResolvedToday = resolvedTodayCount,
                    NewTickets = assignedTickets.Count(t => t.Status?.StatusName == "New"),
                    OverdueTickets = GetOverdueTicketsCount(assignedTickets),
                    HighPriorityTickets = assignedTickets.Count(t => t.Priority?.PriorityName == "High"),
                    PendingUserTickets = assignedTickets.Count(t => t.Status?.StatusName == "Pending User"),

                    TicketsResolvedThisWeek = resolvedThisWeekCount,
                    AverageResponseTime = averageResponseTime,

                    RecentMessages = chatMessages.OrderByDescending(c => c.SentAt).Take(5).ToList(),
                    RecentlyUpdatedTickets = assignedTickets
                        .Where(t => t.TicketTimelines.Any())
                        .OrderByDescending(t => t.TicketTimelines.Max(tt => tt.ChangeTime))
                        .Take(3)
                        .ToList(),

                    CurrentStatusFilter = statusFilter,
                    CurrentPriorityFilter = priorityFilter,
                    CurrentSearchTerm = searchTerm,
                    AvailableStatuses = availableStatuses,
                    AvailablePriorities = availablePriorities
                };

                return View(viewModel);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard.";
                return View(new AgentDashboardViewModel());
            }
        }

   
        public async Task<IActionResult> TicketDetails(int id)
        {
            try
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
                    TempData["ErrorMessage"] = "Ticket not found.";
                    return RedirectToAction("Index");
                }

                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                var agent = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (agent == null || ticket.AssignedTo != agent.UserID)
                {
                    TempData["ErrorMessage"] = "You don't have access to this ticket.";
                    return RedirectToAction("Index");
                }

           
                var chatMessages = await _context.ChatMessages
                    .Include(c => c.User)
                    .Include(c => c.User.Role)
                    .Where(c => c.TicketID == id)
                    .OrderBy(c => c.SentAt)
                    .ToListAsync();

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
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while loading ticket details.";
                return RedirectToAction("Index");
            }
        }

        // Update ticket status
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

                var status = await _context.Statuses
                    .FirstOrDefaultAsync(s => s.StatusName == newStatus);

                if (status == null)
                {
                    TempData["ErrorMessage"] = "Invalid status.";
                    return RedirectToAction("Index");
                }

                ticket.StatusID = status.StatusID;

                if (newStatus == "Resolved" || newStatus == "Closed")
                {
                    ticket.ClosedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

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
            catch (Exception)
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
                    return RedirectToAction("TicketDetails", new { id = ticketId });
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
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while adding the comment.";
            }

            return RedirectToAction("TicketDetails", new { id = ticketId });
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

                TempData["SuccessMessage"] = $"Ticket #{ticketId} priority updated to {priority}.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while updating the priority.";
            }

            return RedirectToAction("Index");
        }

        // KNOWLEDGE BASE FUNCTIONALITY

        [HttpGet]
        public async Task<IActionResult> KnowledgeBase(int? categoryId = null, string searchTerm = "", string sortBy = "newest")
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                var agent = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Department)
                    .FirstOrDefaultAsync(u => u.Email == userEmail);

                if (agent == null)
                {
                    TempData["ErrorMessage"] = "Agent not found.";
                    return RedirectToAction("Index");
                }

                // Get all knowledge base articles (agents can see all)
                var articlesQuery = _context.KnowledgeBase
                    .Include(kb => kb.Category)
                    .Include(kb => kb.CreatedByUser)
                    .Include(kb => kb.LastUpdatedByUser)
                    .AsQueryable();

                // Apply category filter
                if (categoryId.HasValue)
                {
                    articlesQuery = articlesQuery.Where(kb => kb.CategoryID == categoryId.Value);
                }

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    articlesQuery = articlesQuery.Where(kb =>
                        kb.Title.Contains(searchTerm) ||
                        kb.Content.Contains(searchTerm));
                }

                // Apply sorting
                articlesQuery = sortBy switch
                {
                    "oldest" => articlesQuery.OrderBy(kb => kb.CreatedAt),
                    "title" => articlesQuery.OrderBy(kb => kb.Title),
                    "category" => articlesQuery.OrderBy(kb => kb.Category.CategoryName),
                    "updated" => articlesQuery.OrderByDescending(kb => kb.UpdatedAt),
                    _ => articlesQuery.OrderByDescending(kb => kb.CreatedAt) 
                };

                var articles = await articlesQuery.ToListAsync();

             
                var categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryID.ToString(),
                        Text = c.CategoryName,
                        Selected = c.CategoryID == categoryId
                    })
                    .ToListAsync();

                var categoryCountsQuery = await _context.KnowledgeBase
                    .Include(kb => kb.Category)
                    .GroupBy(kb => kb.Category.CategoryName)
                    .Select(g => new { CategoryName = g.Key, Count = g.Count() })
                    .ToListAsync();

                var categoryCounts = categoryCountsQuery.ToDictionary(cc => cc.CategoryName, cc => cc.Count);

               
                var recentArticles = await _context.KnowledgeBase
                    .Include(kb => kb.Category)
                    .Include(kb => kb.CreatedByUser)
                    .OrderByDescending(kb => kb.CreatedAt)
                    .Take(5)
                    .ToListAsync();

       
                var bookmarkedArticles = new List<KnowledgeBase>();

              
                var recentSearches = new List<string>();

                var model = new KnowledgeBaseViewModel
                {
                    Articles = articles,
                    Categories = categories,
                    CategoryCounts = categoryCounts,
                    RecentArticles = recentArticles,
                    BookmarkedArticles = bookmarkedArticles,
                    RecentSearches = recentSearches,
                    SearchTerm = searchTerm,
                    SortBy = sortBy,
                    SelectedCategoryId = categoryId,
                    SelectedCategoryName = categories.FirstOrDefault(c => c.Selected)?.Text ?? "All Categories",
                    TotalArticles = await _context.KnowledgeBase.CountAsync(),
                    AgentName = agent.FullName
                };

                return View(model);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while loading the knowledge base.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ArticleDetails(int id)
        {
            try
            {
                var article = await _context.KnowledgeBase
                    .Include(kb => kb.Category)
                    .Include(kb => kb.CreatedByUser)
                    .Include(kb => kb.LastUpdatedByUser)
                    .FirstOrDefaultAsync(kb => kb.ArticleID == id);

                if (article == null)
                {
                    TempData["ErrorMessage"] = "Article not found.";
                    return RedirectToAction("KnowledgeBase");
                }

                // Verify agent has access (agents can view all articles)
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                var agent = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (agent == null)
                {
                    TempData["ErrorMessage"] = "Access denied.";
                    return RedirectToAction("Index");
                }

                // Get related articles (same category, excluding current article)
                var relatedArticles = await _context.KnowledgeBase
                    .Include(kb => kb.Category)
                    .Where(kb => kb.CategoryID == article.CategoryID && kb.ArticleID != id)
                    .OrderByDescending(kb => kb.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                // Get related tickets that might have been resolved using this article (placeholder)
                var relatedTickets = new List<Ticket>();

                var model = new ArticleDetailViewModel
                {
                    Article = article,
                    RelatedArticles = relatedArticles,
                    RelatedTickets = relatedTickets,
                    BackUrl = "/AgentDashboard/KnowledgeBase",
                    CanEdit = true, // Agents can edit articles
                    IsBookmarked = false, // Implement bookmarking if needed
                    ViewCount = 0, // Implement view tracking if needed
                    LastViewed = DateTime.UtcNow
                };

                return View(model);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while loading the article.";
                return RedirectToAction("KnowledgeBase");
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateArticle()
        {
            try
            {
                var categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryID.ToString(),
                        Text = c.CategoryName
                    })
                    .ToListAsync();

                var model = new CreateArticleViewModel
                {
                    Categories = categories
                };

                return View(model);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while loading the create article page.";
                return RedirectToAction("KnowledgeBase");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateArticle(CreateArticleViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    // Reload categories if validation fails
                    model.Categories = await _context.Categories
                        .Where(c => c.IsActive)
                        .Select(c => new SelectListItem
                        {
                            Value = c.CategoryID.ToString(),
                            Text = c.CategoryName
                        })
                        .ToListAsync();
                    return View(model);
                }

                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                var agent = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (agent == null)
                {
                    TempData["ErrorMessage"] = "Agent not found.";
                    return RedirectToAction("Index");
                }

                var article = new KnowledgeBase
                {
                    Title = model.Title,
                    Content = model.Content,
                    CategoryID = model.CategoryID,
                    CreatedBy = agent.UserID,
                    LastUpdatedBy = agent.UserID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.KnowledgeBase.Add(article);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Article created successfully!";
                return RedirectToAction("ArticleDetails", new { id = article.ArticleID });
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while creating the article.";

                // Reload categories
                model.Categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryID.ToString(),
                        Text = c.CategoryName
                    })
                    .ToListAsync();

                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditArticle(int id)
        {
            try
            {
                var article = await _context.KnowledgeBase
                    .Include(kb => kb.Category)
                    .FirstOrDefaultAsync(kb => kb.ArticleID == id);

                if (article == null)
                {
                    TempData["ErrorMessage"] = "Article not found.";
                    return RedirectToAction("KnowledgeBase");
                }

                var categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryID.ToString(),
                        Text = c.CategoryName,
                        Selected = c.CategoryID == article.CategoryID
                    })
                    .ToListAsync();

                var model = new CreateArticleViewModel
                {
                    ArticleID = article.ArticleID,
                    Title = article.Title,
                    Content = article.Content,
                    CategoryID = article.CategoryID,
                    Categories = categories
                };

                return View("CreateArticle", model);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while loading the article for editing.";
                return RedirectToAction("KnowledgeBase");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditArticle(CreateArticleViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                   
                    model.Categories = await _context.Categories
                        .Where(c => c.IsActive)
                        .Select(c => new SelectListItem
                        {
                            Value = c.CategoryID.ToString(),
                            Text = c.CategoryName,
                            Selected = c.CategoryID == model.CategoryID
                        })
                        .ToListAsync();
                    return View("CreateArticle", model);
                }

                var article = await _context.KnowledgeBase.FindAsync(model.ArticleID);
                if (article == null)
                {
                    TempData["ErrorMessage"] = "Article not found.";
                    return RedirectToAction("KnowledgeBase");
                }

                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                var agent = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (agent == null)
                {
                    TempData["ErrorMessage"] = "Agent not found.";
                    return RedirectToAction("Index");
                }

                article.Title = model.Title;
                article.Content = model.Content;
                article.CategoryID = model.CategoryID;
                article.LastUpdatedBy = agent.UserID;
                article.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Article updated successfully!";
                return RedirectToAction("ArticleDetails", new { id = article.ArticleID });
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while updating the article.";

             
                model.Categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryID.ToString(),
                        Text = c.CategoryName,
                        Selected = c.CategoryID == model.CategoryID
                    })
                    .ToListAsync();

                return View("CreateArticle", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteArticle(int id)
        {
            try
            {
                var article = await _context.KnowledgeBase.FindAsync(id);
                if (article == null)
                {
                    TempData["ErrorMessage"] = "Article not found.";
                    return RedirectToAction("KnowledgeBase");
                }

                _context.KnowledgeBase.Remove(article);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Article deleted successfully!";
                return RedirectToAction("KnowledgeBase");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the article.";
                return RedirectToAction("KnowledgeBase");
            }
        }

        [HttpGet]
        public async Task<IActionResult> QuickSearch(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    return Json(new { results = new List<object>() });
                }

                var results = await _context.KnowledgeBase
                    .Include(kb => kb.Category)
                    .Include(kb => kb.CreatedByUser)
                    .Where(kb => kb.Title.Contains(query) || kb.Content.Contains(query))
                    .OrderByDescending(kb => kb.CreatedAt)
                    .Take(10)
                    .Select(kb => new
                    {
                        articleId = kb.ArticleID,
                        title = kb.Title,
                        excerpt = kb.Content.Length > 150 ? kb.Content.Substring(0, 150) + "..." : kb.Content,
                        categoryName = kb.Category.CategoryName,
                        createdAt = kb.CreatedAt.ToString("MMM dd, yyyy"),
                        createdBy = kb.CreatedByUser.FullName,
                        relevanceScore = kb.Title.Contains(query) ? 2.0 : 1.0
                    })
                    .ToListAsync();

                return Json(new { results });
            }
            catch (Exception)
            {
                return Json(new { results = new List<object>() });
            }
        }

        // Helper methods
        private double CalculateAverageResponseTime(List<Ticket> tickets)
        {
            var resolvedTickets = tickets.Where(t =>
                t.Status?.StatusName == "Resolved" && t.ClosedAt.HasValue).ToList();

            if (!resolvedTickets.Any())
                return 0;

            var totalHours = resolvedTickets.Sum(t =>
                (t.ClosedAt!.Value - t.CreatedAt).TotalHours);

            return Math.Round(totalHours / resolvedTickets.Count, 1);
        }

        private int GetOverdueTicketsCount(List<Ticket> tickets)
        {
            return tickets.Count(t => {
                if (t.Status?.StatusName == "Resolved" || t.Status?.StatusName == "Closed")
                    return false;

                var slaHours = t.Priority?.PriorityName switch
                {
                    "High" => 4,
                    "Medium" => 24,
                    "Low" => 72,
                    _ => 48
                };

                var slaDeadline = t.CreatedAt.AddHours(slaHours);
                return DateTime.UtcNow > slaDeadline;
            });
        }

        private string GetWorkloadStatus(int ticketCount)
        {
            return ticketCount switch
            {
                <= 3 => "Light",
                <= 7 => "Moderate",
                <= 12 => "Heavy",
                _ => "Critical"
            };
        }

        [HttpGet]
        public async Task<IActionResult> GetTicketStats()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                var agent = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (agent == null)
                {
                    return Json(new { error = "Agent not found" });
                }

                var assignedTickets = await _context.Tickets
                    .Include(t => t.Status)
                    .Include(t => t.Priority)
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
                    highPriority = assignedTickets.Count(t => t.Priority?.PriorityName == "High"),
                    workloadStatus = GetWorkloadStatus(assignedTickets.Count)
                };

                return Json(stats);
            }
            catch (Exception)
            {
                return Json(new { error = "An error occurred while fetching stats" });
            }
        }
    }
}