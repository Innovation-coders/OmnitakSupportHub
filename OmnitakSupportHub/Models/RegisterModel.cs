using System.ComponentModel.DataAnnotations;
using OmnitakSupportHub.Models.Validation;

namespace OmnitakSupportHub.Models
{
    public class RegisterModel
    {
        [Required]
        [Display(Name = "Full Name")]
        [StringLength(100, ErrorMessage = "Full name must be at most 100 characters.")]
        public string FullName { get; set; } = "";

        [Required]
        [Display(Name = "Email Address")]
        [RegularExpression(@"^[a-zA-Z][a-zA-Z0-9._%+-]*@(gmail\.com|omnitak\.com)$",
            ErrorMessage = "Email must start with a letter and use @gmail.com or @omnitak.com")]
        public string Email { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [StrongPassword] // Apply our custom attribute
        public string Password { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";

        [Required]
        [Display(Name = "Department")]
        public string Department { get; set; } = "";
    }
}

