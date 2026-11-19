using System.ComponentModel.DataAnnotations;

namespace RentalPS_BE.DTOs
{
    public class RentalRoomRequest
    {
        [Range(1, int.MaxValue)]
        public int RentalId { get; set; }

        [Range(1, int.MaxValue)]
        public int RoomId { get; set; }
    }
}
