using OmnitakSupportHub.Models;

namespace OmnitakSupportHub.Services
{
    public class DataSeeder
    {
        private readonly OmnitakContext _context;

        public DataSeeder(OmnitakContext context)
        {
            _context = context;
        }

        public void Seed()
        {
            if (!_context.Roles.Any())
            {
                _context.Roles.AddRange(
                    new Role { RoleName = "Support Agent", CanManageTickets = true },
                    new Role { RoleName = "Support Manager", CanApproveUsers = true, CanManageTickets = true, CanViewReports = true }
                );
            }

            if (!_context.Users.Any())
            {
                _context.Users.AddRange(
                    new User
                    {
                        FullName = "John Fixer",
                        Email = "john@support.com",
                        PasswordHash = "hashed-password",
                        HashAlgorithm = "SHA256",
                        IsApproved = true,
                        IsActive = true,
                        RoleID = 1 // Support Agent
                    },
                    new User
                    {
                        FullName = "Mary Manager",
                        Email = "mary@support.com",
                        PasswordHash = "hashed-password",
                        HashAlgorithm = "SHA256",
                        IsApproved = true,
                        IsActive = true,
                        RoleID = 2 // Support Manager
                    }
                );
            }

            if (!_context.Statuses.Any())
            {
                _context.Statuses.AddRange(
                    new Status { StatusName = "Open", IsActive = true },
                    new Status { StatusName = "In Progress", IsActive = true },
                    new Status { StatusName = "Resolved", IsActive = true }
                );
            }

            if (!_context.Priorities.Any())
            {
                _context.Priorities.AddRange(
                    new Priority { PriorityName = "Low", Label = "Low Priority", Weight = 1, IsActive = true },
                    new Priority { PriorityName = "Medium", Label = "Medium Priority", Weight = 2, IsActive = true },
                    new Priority { PriorityName = "High", Label = "High Priority", Weight = 3, IsActive = true }
                );
            }

            if (!_context.Categories.Any())
            {
                _context.Categories.AddRange(
                    new Category { CategoryName = "Network", Description = "Network issues", IsActive = true },
                    new Category { CategoryName = "Hardware", Description = "Device problems", IsActive = true },
                    new Category { CategoryName = "Software", Description = "App-related problems", IsActive = true }
                );
            }

            _context.SaveChanges();
        }
    }
}
