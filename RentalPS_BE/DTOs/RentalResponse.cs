using RentalPSMiniProject.Models;

namespace RentalPS_BE.DTOs
{
    public class RentalResponse
    {
        public int Id { get; set; }
        public string RentalCode { get; set; } = string.Empty;
        public int UserId { get; set; }
        public int ConsoleUnitId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal DurationHour { get; set; }
        public decimal ConsoleRate { get; set; }
        public decimal TotalAmount { get; set; }
        public StatusRental StatusRental { get; set; }
    }
}
