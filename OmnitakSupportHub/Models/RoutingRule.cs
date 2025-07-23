using System.ComponentModel.DataAnnotations;

namespace OmnitakSupportHub.Models
{
    public class RoutingRule
    {
        [Key]
        public int RuleID { get; set; }

        public int CategoryID { get; set; }
        public int TeamID { get; set; }
        
        public virtual Category Category { get; set; } = null!;
        public virtual SupportTeam Team { get; set; } = null!;

        public string? Conditions { get; set; }

        public bool IsActive { get; set; }
    }
}
