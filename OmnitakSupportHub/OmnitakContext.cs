using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using OmnitakSupportHub.Models;

namespace OmnitakSupportHub
{
    public class OmnitakContext : DbContext
    {
        public DbSet<Role> Roles { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<SupportTeam> SupportTeams { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Status> Statuses { get; set; }

        public DbSet<Priority> Priorities { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RoutingRule> RoutingRules { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<KnowledgeBase> KnowledgeBase { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<PasswordReset> PasswordResets { get; set; }
        public DbSet<TicketTimeline> TicketTimelines { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<ITStaff> ITStaff { get; set; }

        public OmnitakContext(DbContextOptions<OmnitakContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Best practice is to group configurations by entity for clarity.

            //== User Configuration ==//
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.UserID);
                entity.HasIndex(u => u.Email).IsUnique();

                // Relationship to Role
                entity.HasOne(u => u.Role)
                      .WithMany(r => r.Users)
                      .HasForeignKey(u => u.RoleID)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relationship to SupportTeam
                entity.HasOne(u => u.Team)
                      .WithMany(t => t.Users)
                      .HasForeignKey(u => u.TeamID)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relationship to Department (from second snippet)
                entity.HasOne(u => u.Department)
                      .WithMany(d => d.Users)
                      .HasForeignKey(u => u.DepartmentId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Self-referencing relationship for user approval (special case from first snippet)
                entity.HasOne(u => u.ApprovedByUser)
                      .WithMany(u => u.ApprovedUsers)
                      .HasForeignKey(u => u.ApprovedBy)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            //== Role Configuration ==//
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(r => r.RoleID);
                entity.HasIndex(r => r.RoleName).IsUnique();
            });

            //== Department Configuration (New from second snippet) ==//
            modelBuilder.Entity<Department>()
                .HasKey(d => d.DepartmentId);

            //== SupportTeam Configuration ==//
            modelBuilder.Entity<SupportTeam>(entity =>
            {
                entity.HasKey(st => st.TeamID);
                entity.HasIndex(st => st.TeamName).IsUnique();

                // Relationship to TeamLead (User)
                entity.HasOne(st => st.TeamLead)
                      .WithMany(u => u.LeadTeams)
                      .HasForeignKey(st => st.TeamLeadID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            //== Category, Status, Priority Configurations (New from second snippet) ==//
            modelBuilder.Entity<Category>()
                .HasKey(c => c.CategoryID);

            modelBuilder.Entity<Status>()
                .HasKey(s => s.StatusID);

            modelBuilder.Entity<Priority>()
                .HasKey(p => p.PriorityID);

            //== RoutingRule Configuration (New from second snippet) ==//
            modelBuilder.Entity<RoutingRule>(entity =>
            {
                entity.HasKey(r => r.RuleID);

                entity.HasOne(r => r.Category)
                      .WithMany(c => c.RoutingRules)
                      .HasForeignKey(r => r.CategoryID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Team)
                      .WithMany(t => t.RoutingRules)
                      .HasForeignKey(r => r.TeamID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            //== Ticket Configuration ==//
            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.HasKey(t => t.TicketID);

                // Relationships to Users
                entity.HasOne(t => t.CreatedByUser)
                      .WithMany(u => u.CreatedTickets)
                      .HasForeignKey(t => t.CreatedBy)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.AssignedToUser)
                      .WithMany(u => u.AssignedTickets)
                      .HasForeignKey(t => t.AssignedTo)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relationship to SupportTeam
                entity.HasOne(t => t.Team)
                      .WithMany(st => st.Tickets)
                      .HasForeignKey(t => t.TeamID)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relationships from second snippet
                entity.HasOne(t => t.Category)
                      .WithMany(c => c.Tickets)
                      .HasForeignKey(t => t.CategoryID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.Status)
                      .WithMany(s => s.Tickets)
                      .HasForeignKey(t => t.StatusID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.Priority)
                      .WithMany(p => p.Tickets)
                      .HasForeignKey(t => t.PriorityID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            //== TicketTimeline Configuration ==//
            modelBuilder.Entity<TicketTimeline>(entity =>
            {
                entity.HasKey(tt => tt.TimelineID);

                // Client-side cascade (special case from first snippet)
                entity.HasOne(tt => tt.Ticket)
                      .WithMany(t => t.TicketTimelines)
                      .HasForeignKey(tt => tt.TicketID)
                      .OnDelete(DeleteBehavior.ClientCascade);
            });

            //== ChatMessage Configuration ==//
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(cm => cm.MessageID);

                entity.HasOne(cm => cm.Ticket)
                      .WithMany(t => t.ChatMessages)
                      .HasForeignKey(cm => cm.TicketID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(cm => cm.User)
                      .WithMany(u => u.ChatMessages)
                      .HasForeignKey(cm => cm.UserID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            //== KnowledgeBase Configuration ==//
            modelBuilder.Entity<KnowledgeBase>(entity =>
            {
                entity.HasKey(kb => kb.ArticleID);

                entity.HasOne(kb => kb.CreatedByUser)
                      .WithMany(u => u.CreatedArticles)
                      .HasForeignKey(kb => kb.CreatedBy)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(kb => kb.LastUpdatedByUser)
                      .WithMany(u => u.UpdatedArticles)
                      .HasForeignKey(kb => kb.LastUpdatedBy)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relationship from second snippet
                entity.HasOne(kb => kb.Category)
                      .WithMany(c => c.KnowledgeBaseArticles)
                      .HasForeignKey(kb => kb.CategoryID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            //== Feedback Configuration ==//
            modelBuilder.Entity<Feedback>(entity =>
            {
                entity.HasKey(f => f.FeedbackID);

                entity.HasOne(f => f.Ticket)
                      .WithMany(t => t.Feedbacks)
                      .HasForeignKey(f => f.TicketID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.User)
                      .WithMany(u => u.Feedbacks)
                      .HasForeignKey(f => f.UserID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            //== PasswordReset Configuration
            modelBuilder.Entity<PasswordReset>(entity =>
            {
                entity.HasKey(pr => pr.Token);

                entity.HasOne(pr => pr.User)
                      .WithMany(u => u.PasswordResets)
                      .HasForeignKey(pr => pr.UserID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            //== AuditLog Configuration
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(al => al.LogID);

                entity.HasOne(al => al.User)
                      .WithMany(u => u.AuditLogs)
                      .HasForeignKey(al => al.UserID)
                      .OnDelete(DeleteBehavior.Restrict);
            });


            //Global Delete Behavior Rule
            // This is a safety net to prevent accidental cascade deletes.
            // Any relationship not explicitly configured above will be set to Restrict.
            foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                if (fk.DeleteBehavior == DeleteBehavior.Cascade)
                {
                    fk.DeleteBehavior = DeleteBehavior.Restrict;
                }
            }

            SeedData(modelBuilder);
            base.OnModelCreating(modelBuilder);
        }


        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Roles with enhanced permissions for approval workflow
            modelBuilder.Entity<Role>().HasData(
                new Role
                {
                    RoleID = 1,
                    RoleName = "Administrator",
                    Description = "Full system access including user approval and system management",
                    CanApproveUsers = true,
                    CanManageTickets = true,
                    CanViewAllTickets = true,
                    CanManageKnowledgeBase = true,
                    CanViewReports = true,
                    CanManageTeams = true,
                    IsSystemRole = true
                },
                new Role
                {
                    RoleID = 2,
                    RoleName = "Support Manager",
                    Description = "Can manage support teams and approve users",
                    CanApproveUsers = true,
                    CanManageTickets = true,
                    CanViewAllTickets = true,
                    CanManageKnowledgeBase = true,
                    CanViewReports = true,
                    CanManageTeams = true,
                    IsSystemRole = true
                },
                new Role
                {
                    RoleID = 3,
                    RoleName = "Support Agent",
                    Description = "Can manage and resolve assigned tickets",
                    CanApproveUsers = false,
                    CanManageTickets = true,
                    CanViewAllTickets = true,
                    CanManageKnowledgeBase = true,
                    CanViewReports = false,
                    CanManageTeams = false,
                    IsSystemRole = true
                },
                new Role
                {
                    RoleID = 4,
                    RoleName = "End User",
                    Description = "Can create and view own tickets",
                    CanApproveUsers = false,
                    CanManageTickets = false,
                    CanViewAllTickets = false,
                    CanManageKnowledgeBase = false,
                    CanViewReports = false,
                    CanManageTeams = false,
                    IsSystemRole = true
                }
            );

            // Seed Support Teams
            modelBuilder.Entity<SupportTeam>().HasData(
                new SupportTeam
                {
                    TeamID = 1,
                    TeamName = "IT Support",
                    Description = "General IT support and troubleshooting",
                    Specialization = "Hardware and Software Support"
                },
                new SupportTeam
                {
                    TeamID = 2,
                    TeamName = "Network Team",
                    Description = "Network infrastructure and connectivity",
                    Specialization = "Network Infrastructure"
                },
                new SupportTeam
                {
                    TeamID = 3,
                    TeamName = "Security Team",
                    Description = "Information security and compliance",
                    Specialization = "Cybersecurity"
                },
                new SupportTeam
                {
                    TeamID = 4,
                    TeamName = "Application Support",
                    Description = "Business application support and maintenance",
                    Specialization = "Software Applications"
                }
            );

            // Seed Admin User - WITHOUT self-reference to avoid circular dependency
            string adminPasswordHash = HashPassword("SuperSecure!2025");
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserID = 1,
                    Email = "admin@omnitak.com",
                    PasswordHash = adminPasswordHash,
                    HashAlgorithm = "SHA256",
                    FullName = "System Administrator",
                    DepartmentId = 1,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsApproved = true,
                    IsActive = true,
                    RoleID = 1,
                    TeamID = 1,
                    ApprovedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    ApprovedBy = null // Set to null to avoid self-reference during seeding
                }
            );

            // Seeded Departments
            modelBuilder.Entity<Department>().HasData(
            new Department { DepartmentId = 1, DepartmentName = "IT", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Department { DepartmentId = 2, DepartmentName = "HR", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Department { DepartmentId = 3, DepartmentName = "Finance", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Department { DepartmentId = 4, DepartmentName = "Operations", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Department { DepartmentId = 5, DepartmentName = "Marketing", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Department { DepartmentId = 6, DepartmentName = "Sales", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryID = 1, CategoryName = "Hardware", Description = "Issues related to physical devices" },
                new Category { CategoryID = 2, CategoryName = "Software", Description = "Issues related to software applications" },
                new Category { CategoryID = 3, CategoryName = "Network", Description = "Issues related to network connectivity" },
                new Category { CategoryID = 4, CategoryName = "Security", Description = "Security-related issues and incidents" }
            );

            // Seed Statuses
            modelBuilder.Entity<Status>().HasData(
                new Status { StatusID = 1, StatusName = "Open" },
                new Status { StatusID = 2, StatusName = "In Progress" },
                new Status { StatusID = 3, StatusName = "Resolved" },
                new Status { StatusID = 4, StatusName = "Closed" }
            );

            // Seed Priorities
            modelBuilder.Entity<Priority>().HasData(
                new Priority { PriorityID = 1, PriorityName = "Low" },
                new Priority { PriorityID = 2, PriorityName = "Medium" },
                new Priority { PriorityID = 3, PriorityName = "High", },
                new Priority { PriorityID = 4, PriorityName = "Critical" }
            );

            // Seed RoutingRules
            modelBuilder.Entity<RoutingRule>().HasData(
                new RoutingRule { RuleID = 1, CategoryID = 1, TeamID = 1 }, // Hardware ➜ IT Support
                new RoutingRule { RuleID = 2, CategoryID = 2, TeamID = 1 }, // Software ➜ IT Support
                new RoutingRule { RuleID = 3, CategoryID = 3, TeamID = 2 }, // Network ➜ Network Team
                new RoutingRule { RuleID = 4, CategoryID = 4, TeamID = 3 }  // Security ➜ Security Team
            );

        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                // Changed to match AuthService: salt + password
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes("OmnitakSalt2024" + password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}