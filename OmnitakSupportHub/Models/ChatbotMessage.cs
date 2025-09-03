using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmnitakSupportHub.Models
{
    public class ChatbotMessage
    {
        [Key]
        public int MessageID { get; set; }

        public int ConversationID { get; set; }
        public virtual ChatbotConversation Conversation { get; set; } = null!;

        [Required, Column(TypeName = "nvarchar(max)")]
        public string Message { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string MessageType { get; set; } = string.Empty; // "user", "bot", "system"

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public int? RelatedKnowledgeArticleID { get; set; }
        public virtual KnowledgeBase? RelatedKnowledgeArticle { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? KnowledgeBaseResponse { get; set; }

        public bool IsHelpful { get; set; } = false;
        public DateTime? FeedbackAt { get; set; }
        public decimal? ConfidenceScore { get; set; }
    }
}