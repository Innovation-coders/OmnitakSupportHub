using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;

namespace OmnitakSupportHub.Services
{
    public interface ISimpleChatbotService
    {
        Task<string> ProcessMessageAsync(string userMessage, string sessionID, int? userID = null);
        Task<ChatbotConversation> StartConversationAsync(string sessionID, int? userID = null);
    }

    public class SimpleChatbotService : ISimpleChatbotService
    {
        private readonly OmnitakContext _context;
        private readonly ILogger<SimpleChatbotService> _logger;

        public SimpleChatbotService(OmnitakContext context, ILogger<SimpleChatbotService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> ProcessMessageAsync(string userMessage, string sessionID, int? userID = null)
        {
            try
            {
                // Get or create conversation
                var conversation = await GetConversationAsync(sessionID);
                if (conversation == null)
                {
                    conversation = await StartConversationAsync(sessionID, userID);
                }

                // Save user message
                await SaveMessageAsync(conversation.ConversationID, userMessage, "user");

                // Simple response logic
                var response = GenerateSimpleResponse(userMessage);

                // Save bot response
                await SaveMessageAsync(conversation.ConversationID, response, "bot");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                return "I'm experiencing technical difficulties. Please try creating a support ticket for immediate assistance.";
            }
        }

        public async Task<ChatbotConversation> StartConversationAsync(string sessionID, int? userID = null)
        {
            var conversation = new ChatbotConversation
            {
                SessionID = sessionID,
                UserID = userID,
                StartedAt = DateTime.UtcNow,
                Status = "Active"
            };

            _context.ChatbotConversations.Add(conversation);
            await _context.SaveChangesAsync();

            // Save welcome message
            var welcomeMessage = "Hello! I'm your IT Support Assistant. How can I help you today?";
            await SaveMessageAsync(conversation.ConversationID, welcomeMessage, "bot");

            return conversation;
        }

        private async Task<ChatbotConversation?> GetConversationAsync(string sessionID)
        {
            return await _context.ChatbotConversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.SessionID == sessionID && c.Status == "Active");
        }

        private async Task SaveMessageAsync(int conversationID, string message, string messageType)
        {
            var chatMessage = new ChatbotMessage
            {
                ConversationID = conversationID,
                Message = message,
                MessageType = messageType,
                SentAt = DateTime.UtcNow
            };

            _context.ChatbotMessages.Add(chatMessage);
            await _context.SaveChangesAsync();
        }

        private string GenerateSimpleResponse(string userMessage)
        {
            var message = userMessage.ToLowerInvariant();

            // Simple keyword-based responses
            if (message.Contains("hello") || message.Contains("hi"))
                return "Hello! How can I help you with your IT support needs today?";

            if (message.Contains("password"))
                return "For password reset issues, you can contact IT support or check our knowledge base for self-service options. Would you like me to create a support ticket?";

            if (message.Contains("email"))
                return "For email-related problems, I recommend checking our knowledge base first. If you need immediate assistance, I can help you create a support ticket.";

            if (message.Contains("thank"))
                return "You're welcome! Is there anything else I can help you with?";

            // Default response
            return "I understand you're having an issue. Let me search our knowledge base for relevant information, or I can help you create a support ticket for personalized assistance.";
        }
    }
}