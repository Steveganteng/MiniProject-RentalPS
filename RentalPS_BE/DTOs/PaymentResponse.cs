using RentalPSMiniProject.Models;

namespace RentalPS_BE.DTOs
{
    public class PaymentResponse
    {
        public int Id { get; set; }
        public int RentalId { get; set; }
        public string PaymentCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}
