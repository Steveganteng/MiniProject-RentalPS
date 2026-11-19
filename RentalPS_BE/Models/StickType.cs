using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalPSMiniProject.Models
{
    public class StickType
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(ConsoleType))]
        public int ConsoleTypeId { get; set; }
        public ConsoleType? ConsoleType { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public decimal AdditionalHourlyRate { get; set; }

        public Boolean IsActive { get; set; }

        public ICollection<Stick> Sticks { get; set; } = new List<Stick>();
    }
}
