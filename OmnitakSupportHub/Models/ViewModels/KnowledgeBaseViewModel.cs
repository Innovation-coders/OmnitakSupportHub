// File: Models/ViewModels/KnowledgeBaseViewModel.cs
using Microsoft.AspNetCore.Mvc.Rendering;
using OmnitakSupportHub.Models;
using System.ComponentModel.DataAnnotations;

namespace OmnitakSupportHub.Models.ViewModels
{
    public class KnowledgeBaseViewModel
    {
        // Search and Filter Properties
        public string SearchTerm { get; set; } = string.Empty;
        public int? SelectedCategoryId { get; set; }
        public string SortBy { get; set; } = "Relevance"; // Relevance, Date, Title, Popular

        // Articles Data
        public List<KnowledgeBase> Articles { get; set; } = new();
        public List<KnowledgeBase> PopularArticles { get; set; } = new();
        public List<KnowledgeBase> RecentArticles { get; set; } = new();
        public List<KnowledgeBase> RecommendedArticles { get; set; } = new();

        // Filter Options
        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> SortOptions { get; set; } = new()
        {
            new SelectListItem { Value = "Relevance", Text = "Most Relevant" },
            new SelectListItem { Value = "Date", Text = "Newest First" },
            new SelectListItem { Value = "Title", Text = "Alphabetical" },
            new SelectListItem { Value = "Popular", Text = "Most Popular" }
        };

        // Statistics
        public int TotalArticles { get; set; }
        public int ArticlesInCategory { get; set; }
        public string? SelectedCategoryName { get; set; }

        // Quick Access Categories
        public Dictionary<string, int> CategoryCounts { get; set; } = new();

        // Agent Context (for personalization)
        public string AgentName { get; set; } = string.Empty;
        public List<string> RecentSearches { get; set; } = new();
        public List<KnowledgeBase> BookmarkedArticles { get; set; } = new();
    }

    public class ArticleDetailViewModel
    {
        public KnowledgeBase Article { get; set; } = new();
        public List<KnowledgeBase> RelatedArticles { get; set; } = new();
        public bool IsBookmarked { get; set; }
        public int ViewCount { get; set; }
        public DateTime LastViewed { get; set; }

        // Agent Actions
        public bool CanEdit { get; set; }
        public string BackUrl { get; set; } = "/AgentDashboard/KnowledgeBase";

        // Related Tickets (if article was helpful for tickets)
        public List<Ticket> RelatedTickets { get; set; } = new();
    }

    public class QuickSearchViewModel
    {
        public string Query { get; set; } = string.Empty;
        public List<KnowledgeBaseSearchResult> Results { get; set; } = new();
        public int TotalResults { get; set; }
        public bool HasResults => Results.Any();
    }

    public class KnowledgeBaseSearchResult
    {
        public int ArticleID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public double RelevanceScore { get; set; }
        public List<string> HighlightedTerms { get; set; } = new();
    }

    public class CreateArticleViewModel
    {
        public int ArticleID { get; set; } // For editing

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public int CategoryID { get; set; }

        public List<SelectListItem> Categories { get; set; } = new();

        // Tags for better searchability
        public string Tags { get; set; } = string.Empty;

        // Template options for quick article creation
        public string TemplateType { get; set; } = ""; // FAQ, Troubleshooting, Procedure, etc.

        public bool IsEditing => ArticleID > 0;
    }

    // For agent's personal knowledge base interaction tracking
    public class AgentKnowledgeStats
    {
        public int ArticlesViewed { get; set; }
        public int ArticlesCreated { get; set; }
        public int SearchesPerformed { get; set; }
        public List<string> TopSearchTerms { get; set; } = new();
        public List<KnowledgeBase> MostViewedArticles { get; set; } = new();
        public DateTime LastAccessed { get; set; }
    }
}