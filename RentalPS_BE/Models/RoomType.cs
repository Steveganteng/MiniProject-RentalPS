using System.ComponentModel.DataAnnotations;

namespace RentalPSMiniProject.Models
{
    public class RoomType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public decimal AdditionalHourlyRate { get; set; }

        public string Description { get; set; } = string.Empty;

        public Boolean IsActive { get; set; }
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}
