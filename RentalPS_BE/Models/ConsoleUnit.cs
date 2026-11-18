using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalPSMiniProject.Models
{
    public enum StatusConsole
    {
        Available,
        In_Use,
        Maintenance,
        Inactive
    }
    public class ConsoleUnit
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(ConsoleType))]
        public int ConsoleTypeId { get; set; }
        public ConsoleType? ConsoleType { get; set; }

        [ForeignKey(nameof(Room))]
        public int? RoomId { get; set; }
        public Room? Room { get; set; }
        public string UnitCode { get; set; } = string.Empty;

        public StatusConsole StatusConsole { get; set; }

        
    }
}
