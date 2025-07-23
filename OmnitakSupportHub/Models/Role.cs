using System.ComponentModel.DataAnnotations;

namespace OmnitakSupportHub.Models
{
    public class Role
    {
        [Key]
        public int RoleID { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; } = null!;

        [StringLength(200)]
        public string? Description { get; set; }

        // Authentication and permission properties
        public bool CanApproveUsers { get; set; } = false;
        public bool CanManageTickets { get; set; } = false;
        public bool CanViewAllTickets { get; set; } = false;
        public bool CanManageKnowledgeBase { get; set; } = false;
        public bool CanViewReports { get; set; } = false;
        public bool CanManageTeams { get; set; } = false;
        public bool IsSystemRole { get; set; } = false; // Prevents deletion of critical roles

        // Navigation Properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
