using System.ComponentModel.DataAnnotations;

namespace RentalPS_BE.DTOs
{
    public class StickTypeRequest
    {
        [Required(ErrorMessage ="ConsoleTypeId wajib diisi!")]
        [Range(1, int.MaxValue)]
        public int ConsoleTypeId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Range(typeof(decimal), "0", "9999999999.99", ParseLimitsInInvariantCulture = true)]
        public decimal AdditionalHourlyRate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
