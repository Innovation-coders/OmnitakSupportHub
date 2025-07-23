using System.ComponentModel.DataAnnotations;
using System;

namespace OmnitakSupportHub.Models
{
    public class ChatMessage
    {
        [Key]
        public int MessageID { get; set; }

        public int TicketID { get; set; }

        // Make navigation property optional to fix EF Core object initialization issue
        public virtual Ticket? Ticket { get; set; }

        public int UserID { get; set; }

        public virtual User? User { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }

        public DateTime? ReadAt { get; set; }
    }
}
