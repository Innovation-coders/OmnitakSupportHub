using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace OmnitakSupportHub.Models.ViewModels
{
    public class EditUserViewModel
    {
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public required string FullName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Department")]
        public int? DepartmentId { get; set; }

        public List<SelectListItem> AvailableDepartments { get; set; } = new();

        [Required]
        public int RoleId { get; set; }

        public int? TeamId { get; set; }

        public List<SelectListItem> AvailableRoles { get; set; } = new();
    }
}
