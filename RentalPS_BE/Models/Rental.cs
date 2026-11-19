using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalPSMiniProject.Models
{
    public enum StatusRental
    {
        Pending,
        Active,
        Completed,
        Cancelled
    }
    public class Rental
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string RentalCode { get; set; } = string.Empty;

        [ForeignKey(nameof(User))]
        public int UserId { get; set; }
        public User? User { get; set; }

        [ForeignKey(nameof(ConsoleUnit))]
        public int ConsoleUnitId { get; set; }
        public ConsoleUnit? ConsoleUnit { get; set; }

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public decimal DurationHour { get; set; }
        public decimal ConsoleRate { get; set; }
        public decimal TotalAmount { get; set; }
        public StatusRental StatusRental { get; set; }
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
