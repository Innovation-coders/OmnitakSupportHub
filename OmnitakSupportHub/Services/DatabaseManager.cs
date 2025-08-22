using Microsoft.EntityFrameworkCore;

namespace OmnitakSupportHub.Services
{
    public class DatabaseManager
    {
        private readonly OmnitakContext _context;
        private readonly SmartDataSeeder _seeder;
        private readonly MigrationHelper _migrationHelper;
        private readonly ILogger<DatabaseManager> _logger;

        public DatabaseManager(
            OmnitakContext context,
            SmartDataSeeder seeder,
            MigrationHelper migrationHelper,
            ILogger<DatabaseManager> logger)
        {
            _context = context;
            _seeder = seeder;
            _migrationHelper = migrationHelper;
            _logger = logger;
        }

        /// Initialize database with all necessary data
        public async Task InitializeDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Starting database initialization...");

                // 1. Ensure database exists and apply migrations
                await _context.Database.MigrateAsync();
                _logger.LogInformation("Database migrations applied successfully.");

                // 2. Run smart seeding
                await _seeder.SeedAsync();
                _logger.LogInformation("Smart seeding completed.");

                // 3. Run data migration for existing records
                await _seeder.MigrateExistingDataAsync();
                _logger.LogInformation("Data migration completed.");

                _logger.LogInformation("Database initialization completed successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database initialization failed");
                throw;
            }
        }

        /// Reset database
        public async Task ResetDatabaseAsync()
        {
            try
            {
                _logger.LogWarning("RESETTING DATABASE - ALL DATA WILL BE LOST!");

                await _context.Database.EnsureDeletedAsync();
                _logger.LogInformation("Database deleted.");

                await InitializeDatabaseAsync();
                _logger.LogInformation("Database reset and reinitialized.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database reset failed");
                throw;
            }
        }

        /// Get database status information
        public async Task<DatabaseStatus> GetDatabaseStatusAsync()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync();

                var userCount = canConnect ? await _context.Users.CountAsync() : 0;
                var ticketCount = canConnect ? await _context.Tickets.CountAsync() : 0;

                return new DatabaseStatus
                {
                    CanConnect = canConnect,
                    PendingMigrations = pendingMigrations.ToList(),
                    AppliedMigrations = appliedMigrations.ToList(),
                    UserCount = userCount,
                    TicketCount = ticketCount,
                    LastChecked = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting database status");
                return new DatabaseStatus
                {
                    CanConnect = false,
                    Error = ex.Message,
                    LastChecked = DateTime.UtcNow
                };
            }
        }

        /// Run only the seeding without migrations
        public async Task SeedOnlyAsync()
        {
            try
            {
                _logger.LogInformation("Running seeding only...");
                await _seeder.SeedAsync();
                await _seeder.MigrateExistingDataAsync();
                _logger.LogInformation("Seeding completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Seeding failed");
                throw;
            }
        }
    }

    public class DatabaseStatus
    {
        public bool CanConnect { get; set; }
        public List<string> PendingMigrations { get; set; } = new();
        public List<string> AppliedMigrations { get; set; } = new();
        public int UserCount { get; set; }
        public int TicketCount { get; set; }
        public string? Error { get; set; }
        public DateTime LastChecked { get; set; }
    }
}