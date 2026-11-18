using System.ComponentModel.DataAnnotations;

namespace RentalPSMiniProject.Models
{
    public enum Role
    {
        Admin,
        Customer
    }
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Password { get; set; } = string.Empty;

        public Role Role { get; set; }
        public ICollection<Rental> Rentals { get; set; } = new List<Rental>();
    }
}
