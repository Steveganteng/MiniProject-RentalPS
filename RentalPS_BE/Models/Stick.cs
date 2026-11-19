using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalPSMiniProject.Models
{
    public enum StatusStick
    {
        Available,
        In_Use,
        Maintenance,
        Inactive
    }
    public class Stick
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(StickType))]
        public int StickTypeId { get; set; }
        public StickType? StickType { get; set; }

        public string StickCode { get; set; } = string.Empty;

        public StatusStick StatusStick { get; set; }

        
    }
}
