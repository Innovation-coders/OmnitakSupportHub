using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace OmnitakSupportHub.Models
{
    public class Priority
    {
        [Key]
        public int PriorityID { get; set; }

        [Required, StringLength(50)]
        public string PriorityName { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string Label { get; set; } = string.Empty;

        public int Weight { get; set; }

        public bool IsActive { get; set; }

        // Navigation
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
