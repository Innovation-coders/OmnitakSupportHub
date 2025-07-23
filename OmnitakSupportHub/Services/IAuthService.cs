using System.Collections.Generic;
using System.Threading.Tasks;
using OmnitakSupportHub.Models;

namespace OmnitakSupportHub.Services
{
    public interface IAuthService
    {
        Task<AuthResult> RegisterAsync(RegisterModel model);
        Task<AuthResult> LoginAsync(LoginModel model);
        Task<bool> ApproveUserAsync(int userId, int roleId, int approvedById);
        Task<bool> RejectUserAsync(int userId);
        Task<List<User>> GetPendingUsersAsync();
        Task<List<Role>> GetAvailableRolesAsync();
        Task<bool> CreateAuditLogAsync(
            int userId,
            string action,
            string? targetType = null,
            int? targetId = null,
            string? details = null,
            string? ipAddress = null);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);

        Task<bool> ToggleUserActiveAsync(int userId, int performedById);

        Task<User?> GetUserByIdAsync(int userId);

        Task<bool> UpdateUserAsync(int userId, string fullName, string? department, 
                    int roleId, int? teamId, int modifiedById);

        Task<List<User>> GetActiveUsersAsync();
    }
}
