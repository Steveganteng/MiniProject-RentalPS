namespace RentalPS_BE.DTOs
{
    public class ConsoleTypeResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
        public bool IsActive { get; set; }
    }
}
