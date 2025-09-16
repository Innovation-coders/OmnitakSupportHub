using System.ComponentModel.DataAnnotations;

namespace OmnitakSupportHub.Models
{
    
    public class ConversationContext
    {
        public string SessionId { get; set; } = string.Empty;
        public DateTime ConversationStartTime { get; set; }
        public int MessageCount { get; set; }
        public int GibberishCount { get; set; }
        public string LastUserMessage { get; set; } = string.Empty;
        public string LastBotResponse { get; set; } = string.Empty;
        public List<string> ConversationHistory { get; set; } = new();
        public Dictionary<string, object> CustomData { get; set; } = new();

     
        public void AddToHistory(string speaker, string message)
        {
            ConversationHistory.Add($"{speaker}: {message}");

            // Keep only last 20 exchanges (10 back and forth)
            if (ConversationHistory.Count > 20)
            {
                ConversationHistory.RemoveRange(0, 2);
            }
        }

       
        public TimeSpan ConversationDuration => DateTime.UtcNow - ConversationStartTime;
    }

  
    public class KnowledgeBaseResult
    {
        public int ArticleId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Summary { get; set; } = string.Empty;

        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        public decimal ConfidenceScore { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Keywords { get; set; }

        public string ShortSummary => Summary.Length > 150
            ? Summary.Substring(0, 150) + "..."
            : Summary;
    }

 
    public enum MessageType
    {
        Unknown = 0,
        Greeting = 1,
        Question = 2,
        Request = 3,
        Gratitude = 4,
        Farewell = 5,
        Gibberish = 6,
        TechnicalIssue = 7,
        PasswordHelp = 8,
        EmailProblem = 9,
        NetworkIssue = 10,
        SoftwareProblem = 11,
        HardwareProblem = 12,
        GeneralInquiry = 13
    }

  
    public class MessageAnalysis
    {
        public MessageType Type { get; set; } = MessageType.Unknown;
        public decimal Confidence { get; set; }
        public List<string> Keywords { get; set; } = new();
        public List<string> Entities { get; set; } = new();
        public string? DetectedTopic { get; set; }
        public bool RequiresEscalation { get; set; }
        public string? SuggestedResponse { get; set; }
    }

   
    public class ChatbotConfig
    {
        public int MaxConversationHistory { get; set; } = 20;
        public int MaxGibberishAttempts { get; set; } = 3;
        public int TypingDelayMin { get; set; } = 500;
        public int TypingDelayMax { get; set; } = 1500;
        public bool EnableKnowledgeBaseSearch { get; set; } = true;
        public bool EnablePersonality { get; set; } = true;
        public bool EnableEmojis { get; set; } = true;
        public string DefaultLanguage { get; set; } = "en";
    }


    public class QuickAction
    {
        [Required]
        [StringLength(50)]
        public string Label { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Message { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Icon { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        public int Order { get; set; }
        public bool IsActive { get; set; } = true;
    }

   
    public class ChatbotMetrics
    {
        public string SessionId { get; set; } = string.Empty;
        public int TotalMessages { get; set; }
        public int UserMessages { get; set; }
        public int BotMessages { get; set; }
        public int KnowledgeBaseHits { get; set; }
        public int EscalationRequests { get; set; }
        public TimeSpan SessionDuration { get; set; }
        public DateTime SessionStart { get; set; }
        public DateTime? SessionEnd { get; set; }
        public decimal UserSatisfactionScore { get; set; }
        public List<string> TopicsDiscussed { get; set; } = new();
        public List<string> IssuesResolved { get; set; } = new();
    }

   
    public class ChatbotResponse
    {
        [Required]
        public string Message { get; set; } = string.Empty;

        public MessageType ResponseType { get; set; }
        public decimal Confidence { get; set; }
        public bool RequiresFollowUp { get; set; }
        public bool SuggestEscalation { get; set; }
        public List<KnowledgeBaseResult>? RelatedArticles { get; set; }
        public List<QuickAction>? SuggestedActions { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

  
    public class ConversationStats
    {
        public int TotalConversations { get; set; }
        public int ActiveConversations { get; set; }
        public int CompletedConversations { get; set; }
        public int EscalatedConversations { get; set; }
        public double AverageConversationLength { get; set; }
        public double AverageResponseTime { get; set; }
        public double UserSatisfactionAverage { get; set; }
        public List<string> TopIssueCategories { get; set; } = new();
        public Dictionary<string, int> ResponseTypeFrequency { get; set; } = new();
    }

   
    public static class ChatbotExtensions
    {
       
        public static bool IsTechnicalIssue(this MessageType messageType)
        {
            return messageType switch
            {
                MessageType.TechnicalIssue or
                MessageType.PasswordHelp or
                MessageType.EmailProblem or
                MessageType.NetworkIssue or
                MessageType.SoftwareProblem or
                MessageType.HardwareProblem => true,
                _ => false
            };
        }

        /// <summary>
        /// Gets a user-friendly description of the message type
        /// </summary>
        public static string GetDescription(this MessageType messageType)
        {
            return messageType switch
            {
                MessageType.Greeting => "User greeting",
                MessageType.Question => "User question",
                MessageType.Request => "Service request",
                MessageType.Gratitude => "Thank you message",
                MessageType.Farewell => "Goodbye message",
                MessageType.Gibberish => "Unclear message",
                MessageType.TechnicalIssue => "Technical problem",
                MessageType.PasswordHelp => "Password assistance",
                MessageType.EmailProblem => "Email issue",
                MessageType.NetworkIssue => "Network problem",
                MessageType.SoftwareProblem => "Software issue",
                MessageType.HardwareProblem => "Hardware issue",
                MessageType.GeneralInquiry => "General question",
                _ => "Unknown message type"
            };
        }

      
        public static bool ShowsFrustration(this ConversationContext context)
        {
            return context.GibberishCount > 2 ||
                   context.MessageCount > 10 ||
                   context.ConversationDuration.TotalMinutes > 15;
        }

      
        public static List<QuickAction> GetSuggestedQuickActions(this MessageType messageType)
        {
            return messageType switch
            {
                MessageType.PasswordHelp => new List<QuickAction>
                {
                    new() { Label = "🔐 Reset Password", Message = "I need to reset my password", Category = "Password" },
                    new() { Label = "🔑 Account Locked", Message = "My account is locked", Category = "Password" },
                    new() { Label = "📞 Call IT Support", Message = "I need to speak with IT support", Category = "Escalation" }
                },
                MessageType.EmailProblem => new List<QuickAction>
                {
                    new() { Label = "📧 Can't Send Email", Message = "I can't send emails", Category = "Email" },
                    new() { Label = "📨 Not Receiving Email", Message = "I'm not receiving emails", Category = "Email" },
                    new() { Label = "⚙️ Outlook Issues", Message = "Outlook is not working properly", Category = "Email" }
                },
                MessageType.NetworkIssue => new List<QuickAction>
                {
                    new() { Label = "🌐 No Internet", Message = "I have no internet connection", Category = "Network" },
                    new() { Label = "📶 Slow Connection", Message = "Internet is very slow", Category = "Network" },
                    new() { Label = "🔌 Network Troubleshooting", Message = "Help me troubleshoot network issues", Category = "Network" }
                },
                _ => new List<QuickAction>
                {
                    new() { Label = "🎫 Create Ticket", Message = "I need to create a support ticket", Category = "General" },
                    new() { Label = "📚 Browse Knowledge Base", Message = "I want to search the knowledge base", Category = "General" },
                    new() { Label = "👤 Speak to Agent", Message = "I need to talk to a human agent", Category = "Escalation" }
                }
            };
        }
    }
}