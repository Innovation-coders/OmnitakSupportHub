public class FeedbackDto
{
    public int FeedbackID { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TicketID { get; set; }
    public int UserID { get; set; }
}
