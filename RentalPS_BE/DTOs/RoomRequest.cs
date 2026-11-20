using RentalPSMiniProject.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RentalPS_BE.DTOs
{
    public class RoomRequest
    {
        [Range(1, int.MaxValue)]
        public int RoomTypeId { get; set; }

        [Required]
        [MaxLength(20)]
        public string RoomNumber { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        [EnumDataType(typeof(StatusRoom))]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public StatusRoom StatusRoom { get; set; } = StatusRoom.Available;
    }
}
