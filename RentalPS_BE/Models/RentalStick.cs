using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalPSMiniProject.Models
{
    public class RentalStick
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Rental))]
        public int RentalId { get; set; }
        public Rental? Rental { get; set; }

        [ForeignKey(nameof(Stick))]
        public int StickId { get; set; }
        public Stick? Stick { get; set; }

        public decimal HourlyRate { get; set; }
        public decimal SubTotal { get; set; }
        

    }
}
