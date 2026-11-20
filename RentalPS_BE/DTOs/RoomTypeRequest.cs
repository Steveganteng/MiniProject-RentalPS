using System.ComponentModel.DataAnnotations;

namespace RentalPS_BE.DTOs
{
    public class RoomTypeRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(typeof(decimal), "0", "9999999999.99")]
        public decimal AdditionalHourlyRate { get; set; }

        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}