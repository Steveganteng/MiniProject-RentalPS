using RentalPSMiniProject.Models;

namespace RentalPS_BE.Services
{
    /// <summary>
    /// Satu sumber perhitungan harga Rental PS berdasarkan durasi dan jenis komponen.
    /// Console dan Room VIP mendapatkan diskon durasi; Stick tidak mendapatkan diskon.
    /// </summary>
    public interface IRentalPricingService
    {
        decimal CalculateDurationHours(DateTime startTimeUtc, DateTime endTimeUtc);

        decimal CalculateConsoleSubtotal(decimal hourlyRate, Rental rental);

        decimal CalculateRoomSubtotal(decimal hourlyRate, string roomTypeName, Rental rental);

        decimal CalculateStickSubtotal(decimal hourlyRate, Rental rental);

        decimal CalculateTotal(
            Rental rental,
            IEnumerable<RentalRoom> rentalRooms,
            IEnumerable<RentalStick> rentalSticks);
    }
}
