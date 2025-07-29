using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OmnitakSupportHub.ViewModels
{
    public class ITStaffSpecializationViewModel
    {
        [Required(ErrorMessage = "Please select a user.")]
        public int SelectedUserId { get; set; }

        [Display(Name = "Select Existing Team")]
        public int SelectedSupportTeamId { get; set; }

        [Display(Name = "Or Create New Team")]
        public string? NewSupportTeamName { get; set; }

        public List<SelectListItem> AvailableUsers { get; set; } = new();
        public List<SelectListItem> AvailableSupportTeams { get; set; } = new();
    }
}
