using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;
using OmnitakSupportHub.Models.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;


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


        [HttpGet]
        public async Task<IActionResult> KnowledgeBase(int? categoryId = null, string searchTerm = "", string sortBy = "newest")
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found. Please log in again.";
                    return RedirectToAction("Index");
                }

                // Build articles query - End users can only view articles, not edit them
                var articlesQuery = _context.KnowledgeBase
                    .Include(kb => kb.Category)
                    .Include(kb => kb.CreatedByUser)
                    .Include(kb => kb.LastUpdatedByUser)
                    .AsQueryable();

                // Filter by category
                if (categoryId.HasValue)
                {
                    articlesQuery = articlesQuery.Where(kb => kb.CategoryID == categoryId.Value);
                }

                // Filter by search term
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    articlesQuery = articlesQuery.Where(kb =>
                        kb.Title.Contains(searchTerm) ||
                        kb.Content.Contains(searchTerm));
                }

                // Apply sorting
                articlesQuery = sortBy.ToLower() switch
                {
                    "title" => articlesQuery.OrderBy(kb => kb.Title),
                    "category" => articlesQuery.OrderBy(kb => kb.Category!.CategoryName),
                    "updated" => articlesQuery.OrderByDescending(kb => kb.UpdatedAt),
                    _ => articlesQuery.OrderByDescending(kb => kb.CreatedAt) // newest first
                };

                var articles = await articlesQuery.ToListAsync();

                // Get categories for filtering
                var categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryID.ToString(),
                        Text = c.CategoryName,
                        Selected = c.CategoryID == categoryId
                    })
                    .ToListAsync();

                // Get category counts
                var categoryCountsQuery = await _context.KnowledgeBase
                    .Include(kb => kb.Category)
                    .Where(kb => kb.Category != null)
                    .GroupBy(kb => kb.Category!.CategoryName)
                    .Select(g => new { CategoryName = g.Key, Count = g.Count() })
                    .ToListAsync();

                var categoryCounts = categoryCountsQuery.ToDictionary(cc => cc.CategoryName, cc => cc.Count);

                // Get recent articles for sidebar
                var recentArticles = await _context.KnowledgeBase
                    .Include(kb => kb.Category)
                    .Include(kb => kb.CreatedByUser)
                    .OrderByDescending(kb => kb.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                var model = new UserKnowledgeBaseViewModel
                {
                    Articles = articles,
                    Categories = categories,
                    RecentArticles = recentArticles,
                    SearchTerm = searchTerm ?? string.Empty,
                    SortBy = sortBy ?? "newest",
                    SelectedCategoryId = categoryId,
                    SelectedCategoryName = categories.FirstOrDefault(c => c.Selected)?.Text ?? "All Categories",
                    CategoryCounts = categoryCounts,
                    UserName = user.FullName ?? "User",
                    TotalArticles = await _context.KnowledgeBase.CountAsync()
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

                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
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

                var model = new UserArticleDetailViewModel
                {
                    Article = article,
                    RelatedArticles = relatedArticles,
                    BackUrl = "/UserDashboard/KnowledgeBase",
                    CanEdit = false, // End users cannot edit articles
                    LastViewed = DateTime.UtcNow,
                    WasHelpful = false // TODO: Implement feedback system if needed
                };

                return View(model);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while loading the article.";
                return RedirectToAction("KnowledgeBase");
            }
        }

        // GET: UserDashboard/SearchArticles - for AJAX search
        [HttpGet]
        public async Task<IActionResult> SearchArticles(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    return Json(new { success = false, message = "Search query must be at least 2 characters long." });
                }

                var results = await _context.KnowledgeBase
                    .Include(kb => kb.Category)
                    .Include(kb => kb.CreatedByUser)
                    .Where(kb => kb.Title.Contains(query) || kb.Content.Contains(query))
                    .Select(kb => new
                    {
                        kb.ArticleID,
                        kb.Title,
                        Excerpt = kb.Content.Length > 150 ? kb.Content.Substring(0, 150) + "..." : kb.Content,
                        CategoryName = kb.Category != null ? kb.Category.CategoryName : "Uncategorized",
                        kb.CreatedAt,
                        CreatedBy = kb.CreatedByUser != null ? kb.CreatedByUser.FullName : "Unknown"
                    })
                    .OrderByDescending(kb => kb.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                return Json(new { success = true, results });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Search failed. Please try again." });
            }
        }
        // REPLACE YOUR EXISTING Profile methods in UserDashboardController.cs with these:

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            try
            {
                int userId = int.Parse(User.FindFirst("UserID")?.Value ?? "0");
                var user = await _context.Users
                    .Include(u => u.Department)
                    .FirstOrDefaultAsync(u => u.UserID == userId);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Index");
                }

                // Split the full name safely
                var nameParts = user.FullName?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
                var firstName = nameParts.Length > 0 ? nameParts[0] : "";
                var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

                var model = new ProfileViewModel
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = user.Email ?? "",
                    Phone = "", // Add phone to Users table if needed
                    Department = user.Department?.DepartmentName ?? "Marketing", // Default for demo
                    JobTitle = "Employee", // Add job title to Users table if needed
                    AvatarInitials = GetInitials(user.FullName),
                    MemberSince = user.CreatedAt.ToString("MMM yyyy"),
                    AccountStatus = "Active"
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while loading your profile.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    int userId = int.Parse(User.FindFirst("UserID")?.Value ?? "0");
                    var user = await _context.Users.FindAsync(userId);

                    if (user != null)
                    {
                        user.FullName = $"{model.FirstName} {model.LastName}".Trim();
                        user.Email = model.Email;
                        // Add phone and job title fields to Users table if needed

                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = "Your profile has been updated successfully.";
                        return RedirectToAction(nameof(Profile));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "User not found.";
                    }
                }


                model.AvatarInitials = GetInitials($"{model.FirstName} {model.LastName}");
                model.MemberSince = "Jan 2024"; // Default
                model.AccountStatus = "Active";

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while updating your profile.";


                model.AvatarInitials = GetInitials($"{model.FirstName} {model.LastName}");
                model.MemberSince = "Jan 2024";
                model.AccountStatus = "Active";

                return View(model);
            }
        }

        private string GetInitials(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "U";

            var names = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (names.Length >= 2)
                return $"{names[0][0]}{names[1][0]}".ToUpper();

            return names.Length > 0 && names[0].Length >= 2
                ? names[0].Substring(0, 2).ToUpper()
                : (names.Length > 0 ? names[0][0].ToString().ToUpper() : "U");
        }
    }
}