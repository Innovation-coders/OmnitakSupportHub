using Microsoft.EntityFrameworkCore;
using OmnitakSupportHub.Models;

namespace OmnitakSupportHub.Services
{
    public class MigrationHelper
    {
        private readonly OmnitakContext _context;
        private readonly ILogger<MigrationHelper> _logger;

        public MigrationHelper(OmnitakContext context, ILogger<MigrationHelper> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// Safely adds a new field to existing records if it doesn't exist
        public async Task AddFieldIfNotExistsAsync<T>(
            string fieldName,
            Func<T, bool> hasFieldValue,
            Func<T, T> setFieldValue,
            string logMessage = null) where T : class
        {
            try
            {
                var records = await _context.Set<T>().ToListAsync();
                var recordsToUpdate = records.Where(r => !hasFieldValue(r)).ToList();

                if (recordsToUpdate.Any())
                {
                    foreach (var record in recordsToUpdate)
                    {
                        setFieldValue(record);
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Updated {recordsToUpdate.Count} {typeof(T).Name} records with {fieldName}. {logMessage}");
                }
                else
                {
                    _logger.LogInformation($"All {typeof(T).Name} records already have {fieldName} set.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding field {fieldName} to {typeof(T).Name}");
                throw;
            }
        }

        /// Safely adds new lookup data (Categories, Statuses, etc.)
        public async Task AddLookupDataIfNotExistsAsync<T>(
            T[] newItems,
            Func<T, object> keySelector,
            string entityName = null) where T : class
        {
            try
            {
                entityName ??= typeof(T).Name;
                var addedCount = 0;

                foreach (var item in newItems)
                {
                    var key = keySelector(item);
                    var exists = await _context.Set<T>().AnyAsync(e => keySelector(e).Equals(key));

                    if (!exists)
                    {
                        _context.Set<T>().Add(item);
                        addedCount++;
                    }
                }

                if (addedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Added {addedCount} new {entityName} records.");
                }
                else
                {
                    _logger.LogInformation($"All {entityName} records already exist.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding {entityName} lookup data");
                throw;
            }
        }

        /// Checks if a column exists in the database
        public async Task<bool> ColumnExistsAsync(string tableName, string columnName)
        {
            try
            {
                var sql = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = {0} AND COLUMN_NAME = {1}";

                var result = await _context.Database.SqlQueryRaw<int>(sql, tableName, columnName).FirstAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if column {columnName} exists in table {tableName}");
                return false;
            }
        }

        /// Checks if a table exists in the database
        public async Task<bool> TableExistsAsync(string tableName)
        {
            try
            {
                var sql = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = {0}";

                var result = await _context.Database.SqlQueryRaw<int>(sql, tableName).FirstAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if table {tableName} exists");
                return false;
            }
        }
    }
}