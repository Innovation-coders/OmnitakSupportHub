using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OmnitakSupportHub.Models
{
    public class Department
    {
        [Key]
        public int DepartmentId { get; set; }

        [Required]
        [StringLength(50)]
        public string DepartmentName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property (optional)
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
