

namespace OmnitakSupportHub.Models.Dtos
{
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? SessionId { get; set; }
    }
}