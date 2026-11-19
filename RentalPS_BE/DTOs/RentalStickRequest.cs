using System.ComponentModel.DataAnnotations;

namespace RentalPS_BE.DTOs
{
    public class RentalStickRequest
    {
        [Range(1, int.MaxValue)]
        public int RentalId { get; set; }

        [Range(1, int.MaxValue)]
        public int StickId { get; set; }
    }
}
