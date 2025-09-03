namespace OmnitakSupportHub.Models.Dtos
{
    public class ChatbotMessageDto
    {
        public string Message { get; set; } = string.Empty;
        public string SessionID { get; set; } = string.Empty;
        public int? UserID { get; set; }
        public string MessageType { get; set; } = "user"; // user, bot, system
    }

    public class ChatbotResponseDto
    {
        public string Response { get; set; } = string.Empty;
        public List<KnowledgeBaseResult> RelatedArticles { get; set; } = new();
        public bool ShouldEscalate { get; set; } = false;
        public string EscalationReason { get; set; } = string.Empty;
        public decimal ConfidenceScore { get; set; } = 0;
    }

    public class KnowledgeBaseResult
    {
        public int ArticleID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal RelevanceScore { get; set; } = 0;
    }

    public class ChatbotFeedbackDto
    {
        public int MessageID { get; set; }
        public bool IsHelpful { get; set; }
    }
}