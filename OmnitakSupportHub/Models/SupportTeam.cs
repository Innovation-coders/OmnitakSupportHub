using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace OmnitakSupportHub.Models
{
    public class SupportTeam
    {
        [Key]
        public int TeamID { get; set; }

        [Required, StringLength(100)]
        public required string TeamName { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? Specialization { get; set; }

        public int? TeamLeadID { get; set; }
        public virtual User? TeamLead { get; set; }

        // Navigation
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<RoutingRule> RoutingRules { get; set; } = new List<RoutingRule>();
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
