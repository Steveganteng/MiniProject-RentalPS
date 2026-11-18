using System.ComponentModel.DataAnnotations;

namespace RentalPS_BE.DTOs
{
    public class ConsoleTypeRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(typeof(decimal), "0", "9999999999.99")]
        public decimal HourlyRate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
