using System.ComponentModel.DataAnnotations;

namespace RentalPS_BE.DTOs
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Email diperlukan")]
        [EmailAddress(ErrorMessage = "Format email tidak tidak valid")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password diperlukan")]
        [MinLength(6, ErrorMessage = "Password minimal 6 karakter")]
        public string Password { get; set; } = string.Empty;
    }
}