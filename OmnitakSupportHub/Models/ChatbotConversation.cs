using System.ComponentModel.DataAnnotations;

namespace OmnitakSupportHub.Models
{
    public class ChatbotConversation
    {
        [Key]
        public int ConversationID { get; set; }

        public int? UserID { get; set; }
        public virtual User? User { get; set; }

        [Required, StringLength(100)]
        public string SessionID { get; set; } = string.Empty;

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndedAt { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Active";

        public bool EscalatedToTicket { get; set; } = false;
        public int? TicketID { get; set; }
        public virtual Ticket? Ticket { get; set; }

        public virtual ICollection<ChatbotMessage> Messages { get; set; } = new List<ChatbotMessage>();
    }
}