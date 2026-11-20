using System.ComponentModel.DataAnnotations;
using RentalPSMiniProject.Models;

namespace RentalPS_BE.DTOs
{
    public class RentalStatusRequest
    {
        [EnumDataType(typeof(StatusRental))]
        public StatusRental StatusRental { get; set; }
    }
}
