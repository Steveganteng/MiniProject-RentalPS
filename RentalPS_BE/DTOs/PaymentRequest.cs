using System.ComponentModel.DataAnnotations;
using RentalPSMiniProject.Models;

namespace RentalPS_BE.DTOs
{
    public class PaymentRequest
    {
        [Range(1, int.MaxValue)]
        public int RentalId { get; set; }

        [Range(typeof(decimal), "0.01", "999999999999.99", ParseLimitsInInvariantCulture = true)]
        public decimal Amount { get; set; }

        [EnumDataType(typeof(PaymentMethod))]
        public PaymentMethod PaymentMethod { get; set; }
    }
}
