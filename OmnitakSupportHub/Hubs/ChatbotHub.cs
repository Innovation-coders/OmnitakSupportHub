using Microsoft.AspNetCore.SignalR;

namespace OmnitakSupportHub.Hubs
{
    public class ChatbotHub : Hub
    {
        private readonly ILogger<ChatbotHub> _logger;

        public ChatbotHub(ILogger<ChatbotHub> logger)
        {
            _logger = logger;
        }

        public async Task JoinChatSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"ChatSession_{sessionId}");
            _logger.LogInformation("User joined chat session {SessionId}", sessionId);
        }

        public async Task LeaveChatSession(string sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ChatSession_{sessionId}");
            _logger.LogInformation("User left chat session {SessionId}", sessionId);
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("User connected: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("User disconnected: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}