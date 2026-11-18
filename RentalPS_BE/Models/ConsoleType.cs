using System.ComponentModel.DataAnnotations;

namespace RentalPSMiniProject.Models
{
    public class ConsoleType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public decimal HourlyRate { get; set; }

        public Boolean IsActive { get; set; }

        public ICollection<StickType> StickTypes { get; set; } = new List<StickType>();
        
    }
}
