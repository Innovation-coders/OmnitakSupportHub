using System.ComponentModel.DataAnnotations;

namespace OmnitakSupportHub.Models.ViewModels
{
    public class ForgotPasswordModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }
    }
}
public class ResetPasswordModel
{
    [Required]
    public string Email { get; set; }

    [Required]
    public string Token { get; set; }

    [Required, DataType(DataType.Password)]
    public string NewPassword { get; set; }

    [DataType(DataType.Password), Compare("NewPassword")]
    public string ConfirmPassword { get; set; }
}

