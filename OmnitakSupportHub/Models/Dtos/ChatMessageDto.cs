// ChatMessageDto.cs
namespace OmnitakSupportHub.Models.Dtos
{
    public class ChatMessageDto
    {
        public int TicketID { get; set; }
        public int UserID { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public DateTime? ReadAt { get; set; }   
    }
}
