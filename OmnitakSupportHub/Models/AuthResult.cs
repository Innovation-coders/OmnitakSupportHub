using OmnitakSupportHub.Models;    // for User

namespace OmnitakSupportHub.Models
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string? Token { get; set; }
        public User? User { get; set; }
    }
}
