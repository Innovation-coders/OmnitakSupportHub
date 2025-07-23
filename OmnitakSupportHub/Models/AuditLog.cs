using System;
using System.ComponentModel.DataAnnotations;

namespace OmnitakSupportHub.Models
{
    public class AuditLog
    {
        [Key]
        public int LogID { get; set; }

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty;

        [StringLength(50)]
        public string? TargetType { get; set; }

        public int? TargetID { get; set; }

        [StringLength(500)]
        public string? Details { get; set; }

        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

        // Foreign Key
        public int UserID { get; set; }
        public virtual User User { get; set; } = null!;
    }

    /// Common system audit actions for consistency.
    public static class AuditActions
    {
        public const string UserLogin = "USER_LOGIN";
        public const string UserLogout = "USER_LOGOUT";
        public const string UserRegistered = "USER_REGISTERED";
        public const string UserApproved = "USER_APPROVED";
        public const string UserRejected = "USER_REJECTED";
        public const string TicketCreated = "TICKET_CREATED";
        public const string TicketUpdated = "TICKET_UPDATED";
        public const string TicketAssigned = "TICKET_ASSIGNED";
        public const string TicketClosed = "TICKET_CLOSED";
        public const string PasswordChanged = "PASSWORD_CHANGED";
        public const string RoleChanged = "ROLE_CHANGED";
    }
}
