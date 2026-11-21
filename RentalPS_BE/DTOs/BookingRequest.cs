using System.ComponentModel.DataAnnotations;
using RentalPSMiniProject.Models;

namespace RentalPS_BE.DTOs
{
    public class BookingRequest
    {
        [Range(1, int.MaxValue)]
        public int ConsoleUnitId { get; set; }

        [Range(1, int.MaxValue)]
        public int RoomId { get; set; }

        public List<int> StickIds { get; set; } = new();

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        [EnumDataType(typeof(PaymentMethod))]
        public PaymentMethod PaymentMethod { get; set; }
    }
}