using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace OmnitakSupportHub.Models.ViewModels
{
    public class CreateTicketViewModel
    {
        public int TicketID { get; set; } // For editing

        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        public IEnumerable<SelectListItem> Categories { get; set; }

        [Required]
        [Display(Name = "Priority")]
        public int PriorityID { get; set; }
        public IEnumerable<SelectListItem> Priorities { get; set; }

        [Required]
        [Display(Name = "Status")]
        public int StatusID { get; set; }
        public IEnumerable<SelectListItem> Statuses { get; set; }

        [Display(Name = "Assigned Agent")]
        public int? AssignedTo { get; set; }
        public IEnumerable<SelectListItem> Agents { get; set; }
    }
}
