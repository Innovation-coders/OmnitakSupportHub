using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmnitakSupportHub.Models
{
    public class Ticket
    {
        [Key]
        public int TicketID { get; set; }

        [Required(ErrorMessage ="Title is required")] 
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [Column(TypeName = "nvarchar(max)")]
        public string Description { get; set; } = string.Empty;

        // Normalized Foreign Keys
        [Required]
        public int StatusID { get; set; }     // FK to Status table

        [Required]
        public int PriorityID { get; set; }   // FK to Priority table

        [Required]
        public int CategoryID { get; set; }   // FK to Category table

        // Navigation properties for lookups
        public virtual Status Status { get; set; } = null!;
        public virtual Priority Priority { get; set; } = null!;
        public virtual Category Category { get; set; } = null!;

        // Ticket ownership
        [Required]
        public int CreatedBy { get; set; }   // FK to User
        public virtual User CreatedByUser { get; set; } = null!;

        public int? AssignedTo { get; set; }   // FK to User
        public virtual User? AssignedToUser { get; set; }

        public int? TeamID { get; set; }       // FK to SupportTeam
        public virtual SupportTeam? Team { get; set; }

        // Timestamps
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }

        // Related entities
        public virtual ICollection<TicketTimeline> TicketTimelines { get; set; } = new List<TicketTimeline>();
        public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
        public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    }
}
