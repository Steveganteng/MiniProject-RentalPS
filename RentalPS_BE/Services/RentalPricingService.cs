using Microsoft.Extensions.Logging;
using RentalPS_BE.Exceptions;
using RentalPSMiniProject.Models;

namespace RentalPS_BE.Services
{
    /// <summary>
    /// Kalkulator tarif Rental PS berdasarkan harga master data dan diskon durasi.
    /// Diskon berlaku untuk Console dan Room VIP. Tarif Stick tetap flat sesuai generasi PS.
    /// </summary>
    public class RentalPricingService : IRentalPricingService
    {
        private readonly ILogger<RentalPricingService> _logger;

        private const decimal DiscountThreeToFourHours = 0.10m;
        private const decimal DiscountFiveToSevenHours = 0.15m;
        private const decimal DiscountMoreThanEightHours = 0.20m;

        public RentalPricingService(ILogger<RentalPricingService> logger)
        {
            _logger = logger;
        }

        public decimal CalculateDurationHours(DateTime startTimeUtc, DateTime endTimeUtc)
        {
            var startUtc = NormalizeUtc(startTimeUtc);
            var endUtc = NormalizeUtc(endTimeUtc);

            if (endUtc <= startUtc)
            {
                throw new InvalidRentalDurationException("EndTime harus lebih besar daripada StartTime.");
            }

            return (decimal)(endUtc - startUtc).Ticks / TimeSpan.TicksPerHour;
        }

        public decimal CalculateConsoleSubtotal(decimal hourlyRate, Rental rental)
        {
            var discountRate = GetDurationDiscountRate(rental);
            var subtotal = CalculateDiscountedSubtotal(hourlyRate, rental, discountRate);

            _logger.LogDebug(
                "Console pricing calculated for Rental {RentalId}. BaseRate: {BaseRate}, DurationHours: {DurationHours}, DiscountPercent: {DiscountPercent}, SubTotal: {SubTotal}.",
                rental.Id, hourlyRate, rental.DurationHour, discountRate * 100m, subtotal);

            return subtotal;
        }

        public decimal CalculateRoomSubtotal(decimal hourlyRate, string roomTypeName, Rental rental)
        {
            var isVipRoom = !string.IsNullOrWhiteSpace(roomTypeName) &&
                            roomTypeName.Contains("VIP", StringComparison.OrdinalIgnoreCase);

            // Aturan tim hanya menetapkan diskon untuk Room VIP. Room Regular
            // menggunakan tarif master data apa adanya (biasanya 0 jika gratis).
            var discountRate = isVipRoom ? GetDurationDiscountRate(rental) : 0m;
            var subtotal = CalculateDiscountedSubtotal(hourlyRate, rental, discountRate);

            _logger.LogDebug(
                "Room pricing calculated for Rental {RentalId}. RoomType: {RoomType}, BaseRate: {BaseRate}, DurationHours: {DurationHours}, DiscountPercent: {DiscountPercent}, SubTotal: {SubTotal}.",
                rental.Id, roomTypeName, hourlyRate, rental.DurationHour, discountRate * 100m, subtotal);

            return subtotal;
        }

        public decimal CalculateStickSubtotal(decimal hourlyRate, Rental rental)
        {
            // Daftar harga tim tidak memberikan diskon durasi untuk Stick.
            var subtotal = CalculateDiscountedSubtotal(hourlyRate, rental, 0m);

            _logger.LogDebug(
                "Stick pricing calculated for Rental {RentalId}. BaseRate: {BaseRate}, DurationHours: {DurationHours}, DiscountPercent: 0, SubTotal: {SubTotal}.",
                rental.Id, hourlyRate, rental.DurationHour, subtotal);

            return subtotal;
        }

        public decimal CalculateTotal(
            Rental rental,
            IEnumerable<RentalRoom> rentalRooms,
            IEnumerable<RentalStick> rentalSticks)
        {
            ArgumentNullException.ThrowIfNull(rental);
            ArgumentNullException.ThrowIfNull(rentalRooms);
            ArgumentNullException.ThrowIfNull(rentalSticks);

            var consoleTotal = CalculateConsoleSubtotal(rental.ConsoleRate, rental);

            var roomTotal = rentalRooms
                .Where(room => room.RentalId == rental.Id)
                .Sum(room => CalculateRoomSubtotal(
                    room.AdditionalHourlyRate, room.Name, rental));

            var stickTotal = rentalSticks
                .Where(stick => stick.RentalId == rental.Id)
                .Sum(stick => CalculateStickSubtotal(stick.HourlyRate, rental));

            var total = decimal.Round(
                consoleTotal + roomTotal + stickTotal, 2, MidpointRounding.AwayFromZero);

            _logger.LogInformation(
                "Rental {RentalId} price calculated. Console: {ConsoleTotal}, Room: {RoomTotal}, Stick: {StickTotal}, Total: {TotalAmount}.",
                rental.Id, consoleTotal, roomTotal, stickTotal, total);

            return total;
        }

        private static decimal GetDurationDiscountRate(Rental rental)
        {
            ArgumentNullException.ThrowIfNull(rental);

            if (rental.DurationHour <= 0m)
            {
                throw new InvalidRentalDurationException("Durasi Rental harus lebih besar dari 0.");
            }

            // Gunakan jam penuh yang sudah terlewati untuk menentukan tier.
            // Contoh: 4 jam 30 menit tetap berada di tier 3-4 jam.
            var completedHours = decimal.Floor(rental.DurationHour);

            if (completedHours >= 3m && completedHours <= 4m)
            {
                return DiscountThreeToFourHours;
            }

            if (completedHours >= 5m && completedHours <= 7m)
            {
                return DiscountFiveToSevenHours;
            }

            // Aturan tim menyebut "lebih dari 8 jam". Karena itu tepat 8 jam
            // belum mendapat tier 20%; kasus 8 jam perlu dikonfirmasi tim bila ingin
            // dimasukkan ke tier tertentu.
            if (rental.DurationHour > 8m)
            {
                return DiscountMoreThanEightHours;
            }

            return 0m;
        }

        private static decimal CalculateDiscountedSubtotal(
            decimal hourlyRate, Rental rental, decimal discountRate)
        {
            ArgumentNullException.ThrowIfNull(rental);

            if (hourlyRate < 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hourlyRate), "Tarif per jam tidak boleh negatif.");
            }

            if (!rental.StartTime.HasValue || !rental.EndTime.HasValue ||
                rental.EndTime.Value <= rental.StartTime.Value || rental.DurationHour <= 0m)
            {
                throw new InvalidRentalDurationException(
                    "StartTime, EndTime, dan DurationHour Rental harus valid sebelum menghitung harga.");
            }

            var baseAmount = hourlyRate * rental.DurationHour;
            var discountAmount = baseAmount * discountRate;
            var finalAmount = baseAmount - discountAmount;

            return decimal.Round(finalAmount, 2, MidpointRounding.AwayFromZero);
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                // PostgreSQL timestamp values pada project disimpan sebagai UTC.
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }
    }
}
