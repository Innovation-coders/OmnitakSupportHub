using Microsoft.AspNetCore.Mvc;
using OmnitakSupportHub.Services;
using System.Security.Claims;

namespace OmnitakSupportHub.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiChatController : ControllerBase
    {
        private readonly ISimpleChatbotService _chatbotService;
        private readonly ILogger<AiChatController> _logger;

        public AiChatController(ISimpleChatbotService chatbotService, ILogger<AiChatController> logger)
        {
            _chatbotService = chatbotService;
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<ActionResult<object>> SendMessage([FromBody] ChatRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { error = "Message cannot be empty" });
                }

                // Get user ID if authenticated
                var userIdClaim = User.FindFirst("UserID")?.Value;
                int? userId = null;
                if (int.TryParse(userIdClaim, out int parsedUserId))
                {
                    userId = parsedUserId;
                }

                var response = await _chatbotService.ProcessMessageAsync(
                    request.Message,
                    request.SessionId ?? Guid.NewGuid().ToString(),
                    userId);

                return Ok(new { response, timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message");
                return StatusCode(500, new { error = "Unable to process message" });
            }
        }

        [HttpPost("start")]
        public async Task<ActionResult<object>> StartConversation()
        {
            try
            {
                var sessionId = Guid.NewGuid().ToString();

                var userIdClaim = User.FindFirst("UserID")?.Value;
                int? userId = null;
                if (int.TryParse(userIdClaim, out int parsedUserId))
                {
                    userId = parsedUserId;
                }

                var conversation = await _chatbotService.StartConversationAsync(sessionId, userId);

                return Ok(new
                {
                    sessionId,
                    conversationId = conversation.ConversationID,
                    welcomeMessage = "Hello! I'm your IT Support Assistant. How can I help you today?"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting conversation");
                return StatusCode(500, new { error = "Unable to start conversation" });
            }
        }

        [HttpGet("health")]
        public ActionResult Health()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "Omnitak AI Chat Service"
            });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? SessionId { get; set; }
    }
}