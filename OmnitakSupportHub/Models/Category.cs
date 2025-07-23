using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace OmnitakSupportHub.Models
{
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        [Required, StringLength(50)]
        public required string CategoryName { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        public bool IsActive { get; set; }

        // Navigation
        public virtual ICollection<RoutingRule> RoutingRules { get; set; } = new List<RoutingRule>();
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
        public virtual ICollection<KnowledgeBase> KnowledgeBaseArticles { get; set; } = new List<KnowledgeBase>();
    }
}
