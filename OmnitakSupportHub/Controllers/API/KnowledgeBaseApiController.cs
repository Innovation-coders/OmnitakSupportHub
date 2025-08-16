using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;
using OmnitakSupportHub.Models.Dtos;

namespace OmnitakSupportHub.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class KnowledgeBasesApiController : ControllerBase
    {
        private readonly OmnitakContext _context;

        public KnowledgeBasesApiController(OmnitakContext context)
        {
            _context = context;
        }

     
        [HttpGet]
        [Authorize(Roles = "Administrator,Support Agent,Support Manager,End User")]
        public async Task<ActionResult<IEnumerable<object>>> GetAll([FromQuery] string userType = "user")
        {
            try
            {
                var query = _context.KnowledgeBase
                    .Include(kb => kb.Category)
                    .Include(kb => kb.CreatedByUser)
                    .AsQueryable();

                
                var isAgent = User.IsInRole("Support Agent") || User.IsInRole("Support Manager") || User.IsInRole("Administrator");

                if (!isAgent)
                {
                   
                }

                var articles = await query
                    .Select(kb => new
                    {
                        kb.ArticleID,
                        kb.Title,
                        Content = kb.Content.Length > 200 ? kb.Content.Substring(0, 200) + "..." : kb.Content,
                        CategoryName = kb.Category.CategoryName,
                        CategoryID = kb.CategoryID,
                        CreatedAt = kb.CreatedAt,
                        CreatedBy = kb.CreatedByUser.FullName,
                        UpdatedAt = kb.UpdatedAt,
                        CanEdit = isAgent 
                    })
                    .OrderByDescending(kb => kb.CreatedAt)
                    .ToListAsync();

                return Ok(articles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving articles", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Administrator,Support Agent,Support Manager,End User")]
        public async Task<ActionResult<object>> Get(int id)
        {
            try
            {
                var article = await _context.KnowledgeBase
                    .Include(kb => kb.Category)
                    .Include(kb => kb.CreatedByUser)
                    .Include(kb => kb.LastUpdatedByUser)
                    .FirstOrDefaultAsync(kb => kb.ArticleID == id);

                if (article == null)
                    return NotFound(new { error = "Article not found" });

                var isAgent = User.IsInRole("Support Agent") || User.IsInRole("Support Manager") || User.IsInRole("Administrator");

                var result = new
                {
                    article.ArticleID,
                    article.Title,
                    article.Content,
                    CategoryName = article.Category.CategoryName,
                    article.CategoryID,
                    article.CreatedAt,
                    CreatedBy = article.CreatedByUser.FullName,
                    article.UpdatedAt,
                    LastUpdatedBy = article.LastUpdatedByUser.FullName,
                    CanEdit = isAgent
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the article", details = ex.Message });
            }
        }

      
        [HttpGet("search")]
        [Authorize(Roles = "Administrator,Support Agent,Support Manager,End User")]
        public async Task<ActionResult<object>> Search([FromQuery] string query, [FromQuery] int? categoryId = null, [FromQuery] int limit = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    return BadRequest(new { error = "Search query must be at least 2 characters long" });
                }

                var articlesQuery = _context.KnowledgeBase
                    .Include(kb => kb.Category)
                    .Include(kb => kb.CreatedByUser)
                    .Where(kb => kb.Title.Contains(query) || kb.Content.Contains(query));

                if (categoryId.HasValue)
                {
                    articlesQuery = articlesQuery.Where(kb => kb.CategoryID == categoryId.Value);
                }

                var results = await articlesQuery
                    .Select(kb => new
                    {
                        kb.ArticleID,
                        kb.Title,
                        Excerpt = kb.Content.Length > 150 ? kb.Content.Substring(0, 150) + "..." : kb.Content,
                        CategoryName = kb.Category.CategoryName,
                        kb.CreatedAt,
                        CreatedBy = kb.CreatedByUser.FullName,
                        
                        RelevanceScore = kb.Title.Contains(query) ? 2.0 : 1.0
                    })
                    .OrderByDescending(r => r.RelevanceScore)
                    .ThenByDescending(r => r.CreatedAt)
                    .Take(limit)
                    .ToListAsync();

                return Ok(new { results, totalResults = results.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while searching articles", details = ex.Message });
            }
        }

  
        [HttpGet("categories")]
        [Authorize(Roles = "Administrator,Support Agent,Support Manager,End User")]
        public async Task<ActionResult<object>> GetCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .Select(c => new
                    {
                        c.CategoryID,
                        c.CategoryName,
                        ArticleCount = _context.KnowledgeBase.Count(kb => kb.CategoryID == c.CategoryID)
                    })
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving categories", details = ex.Message });
            }
        }

     
        [HttpPost]
        [Authorize(Roles = "Administrator,Support Agent,Support Manager")]
        public async Task<ActionResult<KnowledgeBase>> Create(KnowledgeBaseDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userIdClaim = User.FindFirst("UserID")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return BadRequest(new { error = "Invalid user ID" });
                }

            
                var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryID == dto.CategoryID && c.IsActive);
                if (!categoryExists)
                {
                    return BadRequest(new { error = "Invalid category ID" });
                }

                var entity = new KnowledgeBase
                {
                    Title = dto.Title,
                    Content = dto.Content,
                    CategoryID = dto.CategoryID,
                    CreatedBy = userId,
                    LastUpdatedBy = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.KnowledgeBase.Add(entity);
                await _context.SaveChangesAsync();

            
                var createdArticle = await _context.KnowledgeBase
                    .Include(kb => kb.Category)
                    .Include(kb => kb.CreatedByUser)
                    .Include(kb => kb.LastUpdatedByUser)
                    .FirstOrDefaultAsync(kb => kb.ArticleID == entity.ArticleID);

                return CreatedAtAction(nameof(Get), new { id = entity.ArticleID }, createdArticle);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the article", details = ex.Message });
            }
        }

       
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator,Support Agent,Support Manager")]
        public async Task<IActionResult> Update(int id, KnowledgeBaseDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var entity = await _context.KnowledgeBase.FindAsync(id);
                if (entity == null)
                    return NotFound(new { error = "Article not found" });

                var userIdClaim = User.FindFirst("UserID")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return BadRequest(new { error = "Invalid user ID" });
                }

              
                var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryID == dto.CategoryID && c.IsActive);
                if (!categoryExists)
                {
                    return BadRequest(new { error = "Invalid category ID" });
                }

                entity.Title = dto.Title;
                entity.Content = dto.Content;
                entity.CategoryID = dto.CategoryID;
                entity.LastUpdatedBy = userId;
                entity.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the article", details = ex.Message });
            }
        }

        
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator,Support Agent,Support Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var entity = await _context.KnowledgeBase.FindAsync(id);
                if (entity == null)
                    return NotFound(new { error = "Article not found" });

                _context.KnowledgeBase.Remove(entity);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the article", details = ex.Message });
            }
        }

      
        [HttpPost("feedback")]
        [Authorize(Roles = "Administrator,Support Agent,Support Manager,End User")]
        public async Task<ActionResult> SubmitFeedback([FromBody] ArticleFeedbackDto feedback)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userIdClaim = User.FindFirst("UserID")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return BadRequest(new { error = "Invalid user ID" });
                }

              
                var articleExists = await _context.KnowledgeBase.AnyAsync(kb => kb.ArticleID == feedback.ArticleID);
                if (!articleExists)
                {
                    return NotFound(new { error = "Article not found" });
                }

               
                return Ok(new { success = true, message = "Thank you for your feedback!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while submitting feedback", details = ex.Message });
            }
        }

        [HttpGet("stats")]
        [Authorize(Roles = "Administrator,Support Agent,Support Manager")]
        public async Task<ActionResult<object>> GetStats()
        {
            try
            {
                var totalArticles = await _context.KnowledgeBase.CountAsync();
                var recentArticles = await _context.KnowledgeBase
                    .Where(kb => kb.CreatedAt >= DateTime.Today.AddDays(-7))
                    .CountAsync();

                var categoriesWithCounts = await _context.Categories
                    .Where(c => c.IsActive)
                    .Select(c => new
                    {
                        c.CategoryName,
                        ArticleCount = _context.KnowledgeBase.Count(kb => kb.CategoryID == c.CategoryID)
                    })
                    .ToListAsync();

                var topAuthors = await _context.KnowledgeBase
                    .Include(kb => kb.CreatedByUser)
                    .GroupBy(kb => kb.CreatedByUser.FullName)
                    .Select(g => new
                    {
                        AuthorName = g.Key,
                        ArticleCount = g.Count()
                    })
                    .OrderByDescending(a => a.ArticleCount)
                    .Take(5)
                    .ToListAsync();

                var stats = new
                {
                    totalArticles,
                    recentArticles,
                    categoriesWithCounts,
                    topAuthors
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving stats", details = ex.Message });
            }
        }
    }

   
    public class ArticleFeedbackDto
    {
        public int ArticleID { get; set; }
        public bool WasHelpful { get; set; }
        public string Comments { get; set; } = string.Empty;
    }
}