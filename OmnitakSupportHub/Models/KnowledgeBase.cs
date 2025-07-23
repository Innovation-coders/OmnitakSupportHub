using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmnitakSupportHub.Models
{
    public class KnowledgeBase
    {
        [Key]
        public int ArticleID { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, Column(TypeName = "nvarchar(max)")]
        public string Content { get; set; } = string.Empty;

        // === Foreign Keys ===
        public int CategoryID { get; set; }
        public virtual Category Category { get; set; } = null!;

        public int CreatedBy { get; set; }
        public virtual User CreatedByUser { get; set; } = null!;

        public int LastUpdatedBy { get; set; }
        public virtual User LastUpdatedByUser { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
