using System.ComponentModel.DataAnnotations;
using OmnitakSupportHub.Models;
using System.Collections.Generic;

namespace OmnitakSupportHub.Models.ViewModels
{
    public class EditUserViewModel
    {
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public required string FullName { get; set; }

        [StringLength(50)]
        public string? Department { get; set; }

        [Required]
        public int RoleId { get; set; }

        public int? TeamId { get; set; }

        public List<Role> AvailableRoles { get; set; } = new();
    }
}
