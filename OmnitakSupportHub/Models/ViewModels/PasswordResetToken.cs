namespace OmnitakSupportHub.Models
{
    public class PasswordResetToken
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public DateTime Expiration { get; set; } = DateTime.UtcNow.AddHours(1);
        public bool IsUsed { get; set; } = false;
    }
}
