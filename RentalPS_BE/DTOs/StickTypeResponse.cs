namespace RentalPS_BE.DTOs
{
    public class StickTypeResponse
    {
        public int Id { get; set; }
        public int ConsoleTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal AdditionalHourlyRate { get; set; }
        public bool IsActive { get; set; }
    }
}
