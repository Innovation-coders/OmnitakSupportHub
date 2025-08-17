using System.ComponentModel.DataAnnotations;

namespace OmnitakSupportHub.Models.Dtos
{
    public class KnowledgeBaseDto
    {
        public int ArticleID { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content is required")]
        [MinLength(10, ErrorMessage = "Content must be at least 10 characters long")]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid category")]
        public int CategoryID { get; set; }

    
        public int CreatedBy { get; set; }
        public int LastUpdatedBy { get; set; }
    }

    public class ArticleFeedbackDto
    {
        [Required]
        public int ArticleID { get; set; }

        [Required]
        public bool IsHelpful { get; set; }

        [StringLength(500, ErrorMessage = "Comments cannot exceed 500 characters")]
        public string Comments { get; set; } = string.Empty;

        public int? Rating { get; set; } 
    }

    public class ArticleSearchDto
    {
        public string Query { get; set; } = string.Empty;
        public int? CategoryID { get; set; }
        public string SortBy { get; set; } = "relevance"; 
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}