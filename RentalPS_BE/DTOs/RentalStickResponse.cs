namespace RentalPS_BE.DTOs
{
    public class RentalStickResponse
    {
        public int Id { get; set; }
        public int RentalId { get; set; }
        public int StickId { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal SubTotal { get; set; }
    }
}
