using System.ComponentModel.DataAnnotations;

namespace RentalPS_BE.DTOs
{
    public class RentalRequest
    {
        [Range(1, int.MaxValue)]
        public int UserId { get; set; }

        [Range(1, int.MaxValue)]
        public int ConsoleUnitId { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Range(typeof(decimal), "0.01", "999999999")]
        public decimal DurationHour { get; set; }
    }
}
