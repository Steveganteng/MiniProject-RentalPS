using System.ComponentModel.DataAnnotations;
using RentalPSMiniProject.Models;

namespace RentalPS_BE.DTOs
{
    public class PaymentStatusRequest
    {
        [EnumDataType(typeof(PaymentStatus))]
        public PaymentStatus PaymentStatus { get; set; }
    }
}
