using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace OmnitakSupportHub.Models
{
    public class Status
    {
        [Key]
        public int StatusID { get; set; }

        [Required, StringLength(20)]
        public required string StatusName { get; set; }

        public bool IsActive { get; set; }

        // Navigation
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
