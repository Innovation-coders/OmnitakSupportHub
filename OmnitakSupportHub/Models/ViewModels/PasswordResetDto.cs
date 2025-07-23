public class PasswordResetDto
{
    public Guid Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public int UserID { get; set; }
}
