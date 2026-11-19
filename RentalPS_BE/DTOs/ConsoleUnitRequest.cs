namespace RentalPS_BE.DTOs
{
    public class ConsoleUnitRequest
    {
        public int ConsoleTypeId { get; set; }
        public int? RoomId { get; set; }
        public string UnitCode { get; set; } = string.Empty;
        public string StatusConsole { get; set; } = string.Empty;
    }
}