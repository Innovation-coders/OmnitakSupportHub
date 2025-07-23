using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OmnitakSupportHub.Models.ViewModels
{
    public class AssignTicketViewModel
    {
        public int TicketID { get; set; }

        [Display(Name = "Assign to Agent")]
        [Required]
        public int SelectedAgentID { get; set; }

        public List<SelectListItem> AvailableAgents { get; set; } = new();

        public string? CurrentAssignee { get; set; }
    }
}
