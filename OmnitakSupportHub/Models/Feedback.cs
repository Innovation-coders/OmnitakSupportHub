using System;
using System.ComponentModel.DataAnnotations;

namespace OmnitakSupportHub.Models
{
    public class Feedback
    {
        [Key]
        public int FeedbackID { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(500)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        public int TicketID { get; set; }
        public virtual Ticket Ticket { get; set; } = null!;

        public int UserID { get; set; }
        public virtual User User { get; set; } = null!;
    }
}
