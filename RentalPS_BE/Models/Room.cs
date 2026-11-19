using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalPSMiniProject.Models
{
    public enum StatusRoom
    {
        Available,
        Maintenance,
        Inactive
    }
    public class Room
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(RoomType))]
        public int RoomTypeId { get; set; }
        public RoomType? RoomType { get; set; }

        public string RoomNumber { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public StatusRoom StatusRoom { get; set; }

        public ICollection<ConsoleUnit> ConsoleUnits { get; set; } = new List<ConsoleUnit>();
       
    }
}
