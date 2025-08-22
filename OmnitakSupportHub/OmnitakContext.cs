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

        public OmnitakContext(DbContextOptions<OmnitakContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
         
            ConfigureUserRelationships(modelBuilder);
            ConfigureTicketRelationships(modelBuilder);
            ConfigureOtherRelationships(modelBuilder);

            
            foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                if (fk.DeleteBehavior == DeleteBehavior.Cascade)
                {
                    fk.DeleteBehavior = DeleteBehavior.Restrict;
                }
            }

            

            base.OnModelCreating(modelBuilder);
        }

        private void ConfigureUserRelationships(ModelBuilder modelBuilder)
        {
            
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasOne(u => u.Role)
                      .WithMany(r => r.Users)
                      .HasForeignKey(u => u.RoleID)
                      .OnDelete(DeleteBehavior.Restrict);

            
                entity.HasOne(u => u.Department)
                      .WithMany(d => d.Users)
                      .HasForeignKey(u => u.DepartmentId)
                      .OnDelete(DeleteBehavior.Restrict);

               
                entity.HasOne(u => u.Team)
                      .WithMany(t => t.Users)
                      .HasForeignKey(u => u.TeamID)
                      .OnDelete(DeleteBehavior.Restrict);

             
                entity.HasOne(u => u.ApprovedByUser)
                      .WithMany(u => u.ApprovedUsers)
                      .HasForeignKey(u => u.ApprovedBy)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            
            modelBuilder.Entity<SupportTeam>(entity =>
            {
                entity.HasOne(st => st.TeamLead)
                      .WithMany(u => u.LeadTeams)
                      .HasForeignKey(st => st.TeamLeadID)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureTicketRelationships(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Ticket>(entity =>
            {
              
                entity.HasOne(t => t.CreatedByUser)
                      .WithMany(u => u.CreatedTickets)
                      .HasForeignKey(t => t.CreatedBy)
                      .OnDelete(DeleteBehavior.Restrict);

               
                entity.HasOne(t => t.AssignedToUser)
                      .WithMany(u => u.AssignedTickets)
                      .HasForeignKey(t => t.AssignedTo)
                      .OnDelete(DeleteBehavior.Restrict);

              
                entity.HasOne(t => t.Team)
                      .WithMany(st => st.Tickets)
                      .HasForeignKey(t => t.TeamID)
                      .OnDelete(DeleteBehavior.Restrict);

               
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

         
            modelBuilder.Entity<TicketTimeline>(entity =>
            {
                entity.HasKey(tt => tt.TimelineID);

                
                entity.HasOne(tt => tt.Ticket)
                      .WithMany(t => t.TicketTimelines)
                      .HasForeignKey(tt => tt.TicketID)
                      .OnDelete(DeleteBehavior.ClientCascade);
            });
        }

        private void ConfigureOtherRelationships(ModelBuilder modelBuilder)
        {
          
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

                entity.HasOne(kb => kb.Category)
                      .WithMany(c => c.KnowledgeBaseArticles)
                      .HasForeignKey(kb => kb.CategoryID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

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

         
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(al => al.LogID);

                entity.HasOne(al => al.User)
                      .WithMany(u => u.AuditLogs)
                      .HasForeignKey(al => al.UserID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

           
            modelBuilder.Entity<PasswordReset>(entity =>
            {
                entity.HasKey(pr => pr.Token);

                entity.HasOne(pr => pr.User)
                      .WithMany(u => u.PasswordResets)
                      .HasForeignKey(pr => pr.UserID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            
            modelBuilder.Entity<RoutingRule>(entity =>
            {
                entity.HasKey(rr => rr.RuleID);

                entity.HasOne(rr => rr.Category)
                      .WithMany(c => c.RoutingRules)
                      .HasForeignKey(rr => rr.CategoryID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(rr => rr.Team)
                      .WithMany(st => st.RoutingRules)
                      .HasForeignKey(rr => rr.TeamID)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}