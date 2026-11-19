using System.ComponentModel.DataAnnotations;

namespace RentalPS_BE.DTOs
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Nama diperlukan")]
        [MaxLength(100, ErrorMessage = "Nama maksimal 100 karakter")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email diperlukan")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password diperlukan")]
        [MinLength(6, ErrorMessage = "Password minimal 6 karakter")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konfirmasi password diperlukan")]
        [Compare("Password", ErrorMessage = "Password dan konfirmasi password tidak cocok")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}