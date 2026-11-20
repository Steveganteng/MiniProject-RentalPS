using System.ComponentModel.DataAnnotations;

namespace RentalPS_BE.DTOs
{
    public class RentalRequest
    {
        // UserId tidak diterima dari client; pemilik diambil dari JWT.
        [Range(1, int.MaxValue)]
        public int ConsoleUnitId { get; set; }

        [Required]
        public DateTimeOffset StartTime { get; set; }

        [Required]
        public DateTimeOffset EndTime { get; set; }
    }
}
