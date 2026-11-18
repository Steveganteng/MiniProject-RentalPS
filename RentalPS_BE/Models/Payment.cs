using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalPSMiniProject.Models
{
    public enum PaymentMethod
    {
        Cash,
        Qris,
        Transfer
    }
    public enum PaymentStatus
    {
        Pending,
        Success,
        Failed
    }
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Rental))]
        public int RentalId { get; set; }
        public Rental? Rental { get; set; }

        [Required]
        public string PaymentCode { get; set; } = string.Empty;

        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }

        public DateTime? PaidAt { get; set; }



    }
}
