using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace OmnitakSupportHub.Models.Validation
{
    public class StrongPasswordAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var password = value?.ToString();
            if (string.IsNullOrWhiteSpace(password))
            {
                return new ValidationResult("Password is required.");
            }

            // Minimum length
            if (password.Length < 8)
            {
                return new ValidationResult("Password must be at least 8 characters.");
            }

            // Uppercase letters
            if (!Regex.IsMatch(password, "[A-Z]"))
            {
                return new ValidationResult("Password must contain at least one uppercase letter (A-Z).");
            }

            // Lowercase letters
            if (!Regex.IsMatch(password, "[a-z]"))
            {
                return new ValidationResult("Password must contain at least one lowercase letter (a-z).");
            }

            // Numbers
            if (!Regex.IsMatch(password, "[0-9]"))
            {
                return new ValidationResult("Password must contain at least one number (0-9).");
            }

            // Special characters
            if (!Regex.IsMatch(password, "[^a-zA-Z0-9]"))
            {
                return new ValidationResult("Password must contain at least one special character.");
            }

            // Common weak passwords (case-insensitive)
            var weakPasswords = new[]
            {
                "password", "12345678", "qwertyui", "11111111", "00000000",
                "abcdefgh", "admin123", "letmein", "welcome", "monkey",
                "abc12345", "iloveyou", "1234abcd", "testtest", "password1"
            };
            if (weakPasswords.Any(wp => wp.Equals(password, StringComparison.OrdinalIgnoreCase)))
            {
                return new ValidationResult("Password is too common. Please choose a stronger password.");
            }

            // All identical characters
            if (password.Distinct().Count() == 1)
            {
                return new ValidationResult("Password cannot consist of the same character repeated.");
            }

            // Simple repeated patterns (e.g., abababab, 12341234)
            if (Regex.IsMatch(password, @"^(.+)\1+$"))
            {
                return new ValidationResult("Password cannot be a repeated pattern.");
            }

            // Sequential characters (e.g., abcdefgh, 12345678)
            string lower = password.ToLower();
            string sequence = "abcdefghijklmnopqrstuvwxyz";
            string digits = "0123456789";
            for (int i = 0; i <= sequence.Length - 4; i++)
            {
                string seq = sequence.Substring(i, 4);
                if (lower.Contains(seq)) return new ValidationResult("Password cannot contain sequential letters.");
            }
            for (int i = 0; i <= digits.Length - 4; i++)
            {
                string seq = digits.Substring(i, 4);
                if (password.Contains(seq)) return new ValidationResult("Password cannot contain sequential numbers.");
            }

            return ValidationResult.Success;
        }
    }
}
