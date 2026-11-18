using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalPSMiniProject.Models
{
    public class RentalRoom
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Rental))]
        public int RentalId { get; set; }
        public Rental? Rental { get; set; }

        [ForeignKey(nameof(Room))]
        public int RoomId { get; set; }
        public Room? Room { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public decimal AdditionalHourlyRate { get; set; }
        public decimal SubTotal { get; set; }
      
    }
}
