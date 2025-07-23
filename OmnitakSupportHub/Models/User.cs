using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmnitakSupportHub.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required, StringLength(255), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(128)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string HashAlgorithm { get; set; } = "SHA256";

        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // === Authentication & Approval ===
        public bool IsApproved { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime? ApprovedAt { get; set; }
        public int? ApprovedBy { get; set; }

        // === Foreign Keys ===
        public int? RoleID { get; set; }
        public int? TeamID { get; set; }

        [ForeignKey("Department")]
        public int? DepartmentId { get; set; }

        // === Navigation Properties ===
        public virtual Role? Role { get; set; }
        public virtual SupportTeam? Team { get; set; }
        public virtual Department? Department { get; set; }
        public virtual User? ApprovedByUser { get; set; }

        // Users that this user has approved
        public virtual ICollection<User> ApprovedUsers { get; set; } = new List<User>();

        // === Ticket Relationships ===
        public virtual ICollection<Ticket> CreatedTickets { get; set; } = new List<Ticket>();
        public virtual ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();

        // === Other Relationships ===
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
        public virtual ICollection<PasswordReset> PasswordResets { get; set; } = new List<PasswordReset>();
        public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
        public virtual ICollection<KnowledgeBase> CreatedArticles { get; set; } = new List<KnowledgeBase>();
        public virtual ICollection<KnowledgeBase> UpdatedArticles { get; set; } = new List<KnowledgeBase>();

        // User can lead multiple teams
        public virtual ICollection<SupportTeam> LeadTeams { get; set; } = new List<SupportTeam>();
    }
}
