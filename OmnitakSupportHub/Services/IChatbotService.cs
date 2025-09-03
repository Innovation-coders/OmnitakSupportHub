using OmnitakSupportHub.Models;
using OmnitakSupportHub.Models.Dtos;

namespace OmnitakSupportHub.Services
{
    public interface IChatbotService
    {
        Task<ChatbotResponseDto> ProcessMessageAsync(string userMessage, string sessionID, int? userID = null);
        Task<List<KnowledgeBaseResult>> SearchKnowledgeBaseAsync(string query, int limit = 5);
        Task<bool> ShouldEscalateToHumanAsync(string userMessage, List<KnowledgeBaseResult> searchResults);
        Task<ChatbotConversation> StartConversationAsync(string sessionID, int? userID = null);
        Task<ChatbotConversation?> GetConversationAsync(string sessionID);
        Task SaveMessageAsync(int conversationID, string message, string messageType,
                            int? relatedArticleID = null, string? knowledgeBaseResponse = null,
                            decimal? confidenceScore = null);
        Task EndConversationAsync(string sessionID);
        Task<bool> EscalateToTicketAsync(string sessionID, string escalationReason);
    }
}