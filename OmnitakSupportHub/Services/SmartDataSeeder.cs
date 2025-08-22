using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;
using System.Security.Cryptography;
using System.Text;

namespace OmnitakSupportHub.Services
{
    public class SmartDataSeeder
    {
        private readonly OmnitakContext _context;
        private readonly ILogger<SmartDataSeeder> _logger;

        public SmartDataSeeder(OmnitakContext context, ILogger<SmartDataSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                _logger.LogInformation("Starting smart data seeding...");

                // Ensure database is created
                await _context.Database.EnsureCreatedAsync();

                // Seed in dependency order
                await SeedRolesAsync();
                await SeedDepartmentsAsync();
                await SeedSupportTeamsAsync();
                await SeedCategoriesAsync();
                await SeedStatusesAsync();
                await SeedPrioritiesAsync();
                await SeedUsersAsync();
                await SeedRoutingRulesAsync();

                _logger.LogInformation("Smart data seeding completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during smart data seeding");
                throw;
            }
        }

        private async Task SeedRolesAsync()
        {
            var rolesToSeed = new[]
            {
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
            };

            foreach (var role in rolesToSeed)
            {
                var existingRole = await _context.Roles.FindAsync(role.RoleID);
                if (existingRole == null)
                {
                    _context.Roles.Add(role);
                    _logger.LogInformation($"Added role: {role.RoleName}");
                }
                else
                {
                    // Update existing role properties (optional)
                    existingRole.Description = role.Description;
                    existingRole.CanApproveUsers = role.CanApproveUsers;
                    existingRole.CanManageTickets = role.CanManageTickets;
                    existingRole.CanViewAllTickets = role.CanViewAllTickets;
                    existingRole.CanManageKnowledgeBase = role.CanManageKnowledgeBase;
                    existingRole.CanViewReports = role.CanViewReports;
                    existingRole.CanManageTeams = role.CanManageTeams;
                    existingRole.IsSystemRole = role.IsSystemRole;
                    _logger.LogInformation($"Updated role: {role.RoleName}");
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedDepartmentsAsync()
        {
            var departmentsToSeed = new[]
            {
                new Department { DepartmentId = 1, DepartmentName = "IT", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Department { DepartmentId = 2, DepartmentName = "HR", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Department { DepartmentId = 3, DepartmentName = "Finance", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Department { DepartmentId = 4, DepartmentName = "Operations", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Department { DepartmentId = 5, DepartmentName = "Marketing", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Department { DepartmentId = 6, DepartmentName = "Sales", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            };

            foreach (var department in departmentsToSeed)
            {
                var existingDepartment = await _context.Departments.FindAsync(department.DepartmentId);
                if (existingDepartment == null)
                {
                    _context.Departments.Add(department);
                    _logger.LogInformation($"Added department: {department.DepartmentName}");
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedSupportTeamsAsync()
        {
            var teamsToSeed = new[]
            {
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
            };

            foreach (var team in teamsToSeed)
            {
                var existingTeam = await _context.SupportTeams.FindAsync(team.TeamID);
                if (existingTeam == null)
                {
                    _context.SupportTeams.Add(team);
                    _logger.LogInformation($"Added support team: {team.TeamName}");
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedCategoriesAsync()
        {
            var categoriesToSeed = new[]
            {
                new Category { CategoryID = 1, CategoryName = "Hardware", Description = "Issues related to physical devices" },
                new Category { CategoryID = 2, CategoryName = "Software", Description = "Issues related to software applications" },
                new Category { CategoryID = 3, CategoryName = "Network", Description = "Issues related to network connectivity" },
                new Category { CategoryID = 4, CategoryName = "Security", Description = "Security-related issues and incidents" }
            };

            foreach (var category in categoriesToSeed)
            {
                var existingCategory = await _context.Categories.FindAsync(category.CategoryID);
                if (existingCategory == null)
                {
                    _context.Categories.Add(category);
                    _logger.LogInformation($"Added category: {category.CategoryName}");
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedStatusesAsync()
        {
            var statusesToSeed = new[]
            {
                new Status { StatusID = 1, StatusName = "Open" },
                new Status { StatusID = 2, StatusName = "In Progress" },
                new Status { StatusID = 3, StatusName = "Resolved" },
                new Status { StatusID = 4, StatusName = "Closed" }
            };

            foreach (var status in statusesToSeed)
            {
                var existingStatus = await _context.Statuses.FindAsync(status.StatusID);
                if (existingStatus == null)
                {
                    _context.Statuses.Add(status);
                    _logger.LogInformation($"Added status: {status.StatusName}");
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedPrioritiesAsync()
        {
            var prioritiesToSeed = new[]
            {
                new Priority { PriorityID = 1, PriorityName = "Low" },
                new Priority { PriorityID = 2, PriorityName = "Medium" },
                new Priority { PriorityID = 3, PriorityName = "High" },
                new Priority { PriorityID = 4, PriorityName = "Critical" }
            };

            foreach (var priority in prioritiesToSeed)
            {
                var existingPriority = await _context.Priorities.FindAsync(priority.PriorityID);
                if (existingPriority == null)
                {
                    _context.Priorities.Add(priority);
                    _logger.LogInformation($"Added priority: {priority.PriorityName}");
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedUsersAsync()
        {
            // Check if admin user exists
            var adminExists = await _context.Users.AnyAsync(u => u.Email == "admin@omnitak.com");

            if (!adminExists)
            {
                var adminUser = new User
                {
                    UserID = 1,
                    Email = "admin@omnitak.com",
                    PasswordHash = HashPassword("SuperSecure!2025"),
                    HashAlgorithm = "SHA256",
                    FullName = "System Administrator",
                    DepartmentId = 1,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsApproved = true,
                    IsActive = true,
                    RoleID = 1,
                    TeamID = 1,
                    ApprovedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    ApprovedBy = null
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Added admin user: admin@omnitak.com");
            }
        }

        private async Task SeedRoutingRulesAsync()
        {
            var rulesToSeed = new[]
            {
                new RoutingRule { RuleID = 1, CategoryID = 1, TeamID = 1 }, // Hardware → IT Support
                new RoutingRule { RuleID = 2, CategoryID = 2, TeamID = 1 }, // Software → IT Support
                new RoutingRule { RuleID = 3, CategoryID = 3, TeamID = 2 }, // Network → Network Team
                new RoutingRule { RuleID = 4, CategoryID = 4, TeamID = 3 }  // Security → Security Team
            };

            foreach (var rule in rulesToSeed)
            {
                var existingRule = await _context.RoutingRules.FindAsync(rule.RuleID);
                if (existingRule == null)
                {
                    _context.RoutingRules.Add(rule);
                    _logger.LogInformation($"Added routing rule: Category {rule.CategoryID} → Team {rule.TeamID}");
                }
            }

            await _context.SaveChangesAsync();
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes("OmnitakSalt2024" + password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Method to add new fields to existing entities safely
        public async Task MigrateExistingDataAsync()
        {
            _logger.LogInformation("Starting data migration for existing entities...");

            // Example: Add new fields to existing Users
            var usersWithoutNewFields = await _context.Users
                .Where(u => u.ApprovedAt == null && u.IsApproved == true)
                .ToListAsync();

            foreach (var user in usersWithoutNewFields)
            {
                user.ApprovedAt = user.CreatedAt; // Set a reasonable default
                _logger.LogInformation($"Migrated user: {user.Email}");
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Data migration completed.");
        }
    }
}