using Microsoft.AspNetCore.Mvc.Rendering;
using OmnitakSupportHub.Models;
using System.ComponentModel.DataAnnotations;

namespace OmnitakSupportHub.Models.ViewModels
{
    public class UserManagementViewModel
    {
        public List<User> Users { get; set; } = new List<User>();
        public List<SelectListItem> AvailableRoles { get; set; } = new List<SelectListItem>();
    }

    public class CreateRoleViewModel
    {
        public int RoleID { get; set; }

        [Required(ErrorMessage = "Role name is required")]
        [StringLength(50, ErrorMessage = "Role name cannot exceed 50 characters")]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Can Approve Users")]
        public bool CanApproveUsers { get; set; }

        [Display(Name = "Can Manage Tickets")]
        public bool CanManageTickets { get; set; }

        [Display(Name = "Can View All Tickets")]
        public bool CanViewAllTickets { get; set; }

        [Display(Name = "Can Manage Knowledge Base")]
        public bool CanManageKnowledgeBase { get; set; }

        [Display(Name = "Can View Reports")]
        public bool CanViewReports { get; set; }

        [Display(Name = "Can Manage Teams")]
        public bool CanManageTeams { get; set; }

        public bool IsSystemRole { get; set; }
    }
}