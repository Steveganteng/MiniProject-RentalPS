namespace RentalPS_BE.DTOs
{
    public class ConsoleUnitResponse
    {
        public int Id { get; set; }
        public int ConsoleTypeId { get; set; }
        public string ConsoleTypeName { get; set; } = string.Empty;
        public int? RoomId { get; set; }
        public string? RoomNumber { get; set; }
        public string UnitCode { get; set; } = string.Empty;
        public string StatusConsole { get; set; } = string.Empty;
    }
}