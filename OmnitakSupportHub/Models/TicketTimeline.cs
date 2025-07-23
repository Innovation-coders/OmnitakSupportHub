using System.ComponentModel.DataAnnotations;
using System;

namespace OmnitakSupportHub.Models
{
    public class TicketTimeline
    {
        [Key]
        public int TimelineID { get; set; }

        public int TicketID { get; set; }
        public virtual Ticket? Ticket { get; set; }

        public DateTime? ExpectedResolution { get; set; }
        public DateTime? ActualResolution { get; set; }

        public int ChangedByUserID { get; set; }

        public string? OldStatus { get; set; }
        public string? NewStatus { get; set; }

        public DateTime ChangeTime { get; set; }
    }
}
