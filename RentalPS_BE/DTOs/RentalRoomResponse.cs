namespace RentalPS_BE.DTOs
{
    public class RentalRoomResponse
    {
        public int Id { get; set; }
        public int RentalId { get; set; }
        public int RoomId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal AdditionalHourlyRate { get; set; }
        public decimal SubTotal { get; set; }
    }
}
