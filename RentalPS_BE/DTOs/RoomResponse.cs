using RentalPSMiniProject.Models;
using System.Text.Json.Serialization;

namespace RentalPS_BE.DTOs
{
    public class RoomResponse
    {
        public int Id { get; set; }
        public int RoomTypeId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public StatusRoom StatusRoom { get; set; }
    }
}