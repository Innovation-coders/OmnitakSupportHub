using System;
using System.ComponentModel.DataAnnotations;

namespace OmnitakSupportHub.Models.ViewModels
{
    public class PendingUserViewModel
    {
        public Guid Id { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Requested On")]
        public DateTime RequestDate { get; set; }
    }
}
