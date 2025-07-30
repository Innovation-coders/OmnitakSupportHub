public class TicketTimelineDto
{
    public int TimelineID { get; set; }
    public int TicketID { get; set; }
    public DateTime? ExpectedResolution { get; set; }
    public DateTime? ActualResolution { get; set; }
}
