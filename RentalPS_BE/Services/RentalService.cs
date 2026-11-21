using RentalPS_BE.DTOs;
using RentalPS_BE.Repositories;
using RentalPSMiniProject.Models;
using RentalPS_BE.Exceptions;
using Microsoft.Extensions.Logging;

namespace RentalPS_BE.Services
{
    public class RentalService : IRentalService
    {
        private readonly ILogger<RentalService> _logger;
        private readonly IRepository<Rental> _rentalRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<ConsoleUnit> _consoleUnitRepository;
        private readonly IRepository<Stick> _stickRepository;
        private readonly IRepository<ConsoleType> _consoleTypeRepository;
        private readonly IRepository<RentalRoom> _rentalRoomRepository;
        private readonly IRepository<RentalStick> _rentalStickRepository;
        private readonly IRepository<Payment> _paymentRepository;
        private readonly IRentalPricingService _rentalPricingService;

        public RentalService(
            IRepository<Rental> rentalRepository,
            IRepository<User> userRepository,
            IRepository<ConsoleUnit> consoleUnitRepository,
            IRepository<Stick> stickRepository,
            IRepository<ConsoleType> consoleTypeRepository,
            IRepository<RentalRoom> rentalRoomRepository,
            IRepository<RentalStick> rentalStickRepository,
            IRepository<Payment> paymentRepository,
            IRentalPricingService rentalPricingService,
            ILogger<RentalService> logger)
        {
            _rentalRepository = rentalRepository;
            _userRepository = userRepository;
            _consoleUnitRepository = consoleUnitRepository;
            _stickRepository = stickRepository;
            _consoleTypeRepository = consoleTypeRepository;
            _rentalRoomRepository = rentalRoomRepository;
            _rentalStickRepository = rentalStickRepository;
            _paymentRepository = paymentRepository;
            _rentalPricingService = rentalPricingService;
            _logger = logger;
        }

        public async Task<List<RentalResponse>> GetAllAsync(int userId, bool isAdmin)
        {
            var rentals = await _rentalRepository.GetAllAsync();

            var visibleRentals = isAdmin
                ? rentals
                : rentals.Where(r => r.UserId == userId);

            return visibleRentals
                .OrderByDescending(r => r.Id)
                .Select(ToResponse)
                .ToList();
        }

        public async Task<RentalResponse?> GetByIdAsync(int id, int userId, bool isAdmin)
        {
            var rental = await _rentalRepository.GetByIdAsync(id);

            if (rental == null || (!isAdmin && rental.UserId != userId))
            {
                return null;
            }

            return ToResponse(rental);
        }

        public async Task<RentalResponse> CreateAsync(int userId, RentalRequest request)
        {
            ValidateRequest(request);

            var (user, consoleUnit, consoleType, startTimeUtc, endTimeUtc, durationHour) =
                await ValidateRentalReferencesAsync(userId, request, excludeRentalId: null);

            var rental = new Rental
            {
                RentalCode = GenerateRentalCode(),
                UserId = user.Id,
                ConsoleUnitId = consoleUnit.Id,
                StartTime = startTimeUtc,
                EndTime = endTimeUtc,
                DurationHour = durationHour,
                // Simpan tarif konsol sebagai snapshot saat transaksi dibuat.
                ConsoleRate = consoleType.HourlyRate,
                // Rental baru belum memiliki detail Room/Stick, sehingga total awal
                // dihitung oleh kalkulator bersama dari komponen yang tersedia.
                StatusRental = StatusRental.Pending
            };

            rental.TotalAmount = _rentalPricingService.CalculateTotal(
                rental,
                Array.Empty<RentalRoom>(),
                Array.Empty<RentalStick>());

            await _rentalRepository.AddAsync(rental);
            await _rentalRepository.SaveChangesAsync();
            _logger.LogInformation("Rental {RentalId} ({RentalCode}) created for user {UserId}. ConsoleUnit: {ConsoleUnitId}, Start: {StartTimeUtc}, End: {EndTimeUtc}, DurationHours: {DurationHours}, TotalAmount: {TotalAmount}, Status: {Status}.", rental.Id, rental.RentalCode, rental.UserId, rental.ConsoleUnitId, rental.StartTime, rental.EndTime, rental.DurationHour, rental.TotalAmount, rental.StatusRental);

            return ToResponse(rental);
        }

        public async Task<RentalResponse?> UpdateAsync(int id, int userId, RentalRequest request)
        {
            ValidateRequest(request);

            var rental = await _rentalRepository.GetByIdAsync(id);
            if (rental == null || rental.UserId != userId)
            {
                return null;
            }

            if (rental.StatusRental != StatusRental.Pending)
            {
                throw new ArgumentException(
                    "Rental hanya dapat diubah ketika masih berstatus Pending.");
            }

            var payments = await _paymentRepository.GetAllAsync();
            if (payments.Any(p => p.RentalId == rental.Id))
            {
                throw new ArgumentException(
                    "Rental tidak dapat dibatalkan karena pembayaran sudah berhasil. Saat ini fitur refund belum tersedia. Silakan hubungi Admin untuk bantuan.");
            }

            var (_, consoleUnit, consoleType, startTimeUtc, endTimeUtc, durationHour) =
                await ValidateRentalReferencesAsync(userId, request, excludeRentalId: rental.Id);

            rental.ConsoleUnitId = consoleUnit.Id;
            rental.StartTime = startTimeUtc;
            rental.EndTime = endTimeUtc;
            rental.DurationHour = durationHour;
            rental.ConsoleRate = consoleType.HourlyRate;

            // Pertahankan snapshot tarif detail, tetapi hitung ulang subtotal
            // apabila durasi rental berubah.
            var rentalRooms = await _rentalRoomRepository.GetAllAsync();
            var currentRentalRooms = rentalRooms.Where(rr => rr.RentalId == rental.Id).ToList();
            foreach (var rentalRoom in currentRentalRooms)
            {
                rentalRoom.SubTotal = _rentalPricingService.CalculateRoomSubtotal(
                    rentalRoom.AdditionalHourlyRate, rentalRoom.Name, rental);
                _rentalRoomRepository.Update(rentalRoom);
            }

            var rentalSticks = await _rentalStickRepository.GetAllAsync();
            var currentRentalSticks = rentalSticks.Where(rs => rs.RentalId == rental.Id).ToList();
            foreach (var rentalStick in currentRentalSticks)
            {
                rentalStick.SubTotal = _rentalPricingService.CalculateStickSubtotal(
                    rentalStick.HourlyRate, rental);
                _rentalStickRepository.Update(rentalStick);
            }

            rental.TotalAmount = _rentalPricingService.CalculateTotal(
                rental, currentRentalRooms, currentRentalSticks);

            _rentalRepository.Update(rental);
            // Semua repository menggunakan DbContext scoped yang sama,
            // sehingga satu SaveChanges menyimpan seluruh perubahan di atas.
            await _rentalRepository.SaveChangesAsync();
            _logger.LogInformation("Rental {RentalId} updated by user {UserId}. Start: {StartTimeUtc}, End: {EndTimeUtc}, DurationHours: {DurationHours}, TotalAmount: {TotalAmount}.", rental.Id, userId, rental.StartTime, rental.EndTime, rental.DurationHour, rental.TotalAmount);

            return ToResponse(rental);
        }

        public async Task<bool> CancelAsync(int id, int userId)
        {
            var rental = await _rentalRepository.GetByIdAsync(id);
            if (rental == null || rental.UserId != userId)
            {
                return false;
            }

            if (rental.StatusRental != StatusRental.Pending)
            {
                throw new ArgumentException(
                    "Hanya rental berstatus Pending yang dapat dibatalkan melalui operasi ini.");
            }

            var payments = await _paymentRepository.GetAllAsync();
            if (payments.Any(p => p.RentalId == rental.Id && p.PaymentStatus == PaymentStatus.Success))
            {
                throw new ArgumentException(
                     "Rental tidak dapat dibatalkan karena sudah memiliki pembayaran Success. Saat ini sistem belum menyediakan fitur refund. Silakan hubungi Admin untuk proses pengembalian dana secara manual.");
            }

            // Rental tidak dihapus secara fisik agar histori transaksi tetap tersimpan.
            rental.StatusRental = StatusRental.Cancelled;
            _rentalRepository.Update(rental);
            await _rentalRepository.SaveChangesAsync();
            _logger.LogInformation("Rental {RentalId} cancelled by customer {UserId}.", rental.Id, userId);

            return true;
        }

        public async Task<RentalResponse?> UpdateStatusAsync(
            int id,
            RentalStatusRequest request)
        {
            var rental = await _rentalRepository.GetByIdAsync(id);
            if (rental == null)
            {
                return null;
            }

            if (!Enum.IsDefined(typeof(StatusRental), request.StatusRental))
            {
                throw new ArgumentException("Status rental yang dipilih tidak valid.");
            }

            // Status Rental menggambarkan proses penyewaan, sedangkan PaymentStatus
            // tetap dikelola secara terpisah oleh PaymentService.
            switch (request.StatusRental)
            {
                case StatusRental.Active:
                {
                    if (rental.StatusRental != StatusRental.Pending)
                    {
                        throw new ArgumentException(
                            "Rental hanya dapat diubah menjadi Active apabila status saat ini masih Pending.");
                    }

                    // Saat Admin mengonfirmasi Customer benar-benar menyewa,
                    // tandai inventaris konsol dan stik sebagai sedang digunakan.
                    var consoleUnit = await _consoleUnitRepository.GetByIdAsync(rental.ConsoleUnitId);
                    if (consoleUnit == null)
                    {
                        throw new NotFoundException("Console Unit pada rental tidak ditemukan.");
                    }

                    if (consoleUnit.StatusConsole != StatusConsole.Available)
                    {
                        throw new InventoryNotAvailableException(
                            "Rental tidak dapat diaktifkan karena Console Unit tidak dalam kondisi Available.");
                    }

                    var rentalSticks = (await _rentalStickRepository.GetAllAsync())
                        .Where(detail => detail.RentalId == rental.Id)
                        .ToList();

                    var assignedSticks = new List<Stick>();
                    foreach (var rentalStick in rentalSticks)
                    {
                        var stick = await _stickRepository.GetByIdAsync(rentalStick.StickId);
                        if (stick == null)
                        {
                            throw new NotFoundException(
                                $"Stick dengan ID {rentalStick.StickId} pada rental tidak ditemukan.");
                        }

                        if (stick.StatusStick != StatusStick.Available)
                        {
                            throw new InventoryNotAvailableException(
                                $"Rental tidak dapat diaktifkan karena Stick {stick.StickCode} tidak dalam kondisi Available.");
                        }

                        assignedSticks.Add(stick);
                    }

                    consoleUnit.StatusConsole = StatusConsole.In_Use;
                    _consoleUnitRepository.Update(consoleUnit);

                    foreach (var stick in assignedSticks)
                    {
                        stick.StatusStick = StatusStick.In_Use;
                        _stickRepository.Update(stick);
                    }

                    break;
                }

                case StatusRental.Completed:
                {
                    if (rental.StatusRental != StatusRental.Active)
                    {
                        throw new ArgumentException(
                            "Rental hanya dapat diselesaikan apabila status saat ini Active.");
                    }

                    // Setelah rental selesai, kembalikan inventaris yang ditandai
                    // In_Use oleh rental ini menjadi Available.
                    var consoleUnit = await _consoleUnitRepository.GetByIdAsync(rental.ConsoleUnitId);
                    if (consoleUnit != null && consoleUnit.StatusConsole == StatusConsole.In_Use)
                    {
                        consoleUnit.StatusConsole = StatusConsole.Available;
                        _consoleUnitRepository.Update(consoleUnit);
                    }

                    var rentalSticks = (await _rentalStickRepository.GetAllAsync())
                        .Where(detail => detail.RentalId == rental.Id)
                        .ToList();

                    foreach (var rentalStick in rentalSticks)
                    {
                        var stick = await _stickRepository.GetByIdAsync(rentalStick.StickId);
                        if (stick != null && stick.StatusStick == StatusStick.In_Use)
                        {
                            stick.StatusStick = StatusStick.Available;
                            _stickRepository.Update(stick);
                        }
                    }

                    break;
                }

                case StatusRental.Cancelled:
                    if (rental.StatusRental != StatusRental.Pending)
                    {
                        throw new ArgumentException(
                            "Admin hanya dapat membatalkan rental yang masih berstatus Pending. Rental yang sudah Active harus diselesaikan melalui status Completed.");
                    }

                    var payments = await _paymentRepository.GetAllAsync();
                    var hasSuccessfulPayment = payments.Any(p =>
                        p.RentalId == rental.Id &&
                        p.PaymentStatus == PaymentStatus.Success);

                    if (hasSuccessfulPayment)
                    {
                        throw new ArgumentException(
                            "Rental tidak dapat dibatalkan karena pembayaran sudah berhasil. Saat ini sistem belum menyediakan fitur refund. Silakan hubungi Admin untuk bantuan pengembalian dana.");
                    }
                    break;

                case StatusRental.Pending:
                default:
                    throw new ArgumentException(
                        "Status tujuan tidak valid. Admin hanya dapat mengubah Pending menjadi Active atau Cancelled, dan Active menjadi Completed.");
            }

            var previousStatus = rental.StatusRental;
            rental.StatusRental = request.StatusRental;
            _rentalRepository.Update(rental);
            await _rentalRepository.SaveChangesAsync();
            _logger.LogInformation("Rental {RentalId} status changed from {PreviousStatus} to {NewStatus} by administrator.", rental.Id, previousStatus, rental.StatusRental);

            return ToResponse(rental);
        }

        private async Task<(User User, ConsoleUnit ConsoleUnit, ConsoleType ConsoleType,
            DateTime StartTimeUtc, DateTime EndTimeUtc, decimal DurationHour)>
            ValidateRentalReferencesAsync(int userId, RentalRequest request, int? excludeRentalId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException("UserId tidak ditemukan.");
            }

            var consoleUnit = await _consoleUnitRepository.GetByIdAsync(request.ConsoleUnitId);
            if (consoleUnit == null)
            {
                throw new NotFoundException("ConsoleUnitId tidak ditemukan.");
            }

            // In_Use tidak otomatis menutup kesempatan booking untuk jadwal mendatang.
            // Bentrok waktu diperiksa terpisah oleh EnsureConsoleUnitIsNotBookedAsync.
            if (consoleUnit.StatusConsole is StatusConsole.Maintenance or StatusConsole.Inactive)
            {
                throw new InventoryNotAvailableException(
                    "Console unit sedang Maintenance atau Inactive sehingga tidak dapat dipesan.");
            }

            var consoleType = await _consoleTypeRepository.GetByIdAsync(consoleUnit.ConsoleTypeId);
            if (consoleType == null)
            {
                throw new NotFoundException("ConsoleType untuk unit tersebut tidak ditemukan.");
            }

            if (!consoleType.IsActive)
            {
                throw new ArgumentException("Jenis console tersebut sedang tidak aktif.");
            }

            var startTimeUtc = request.StartTime.UtcDateTime;
            var endTimeUtc = request.EndTime.UtcDateTime;
            var durationHour = _rentalPricingService.CalculateDurationHours(
                startTimeUtc, endTimeUtc);

            await EnsureConsoleUnitIsNotBookedAsync(
                request.ConsoleUnitId,
                startTimeUtc,
                endTimeUtc,
                excludeRentalId);

            return (user, consoleUnit, consoleType, startTimeUtc, endTimeUtc, durationHour);
        }

        private async Task EnsureConsoleUnitIsNotBookedAsync(
            int consoleUnitId,
            DateTime targetStart,
            DateTime targetEnd,
            int? excludeRentalId)
        {
            var rentals = await _rentalRepository.GetAllAsync();

            foreach (var existingRental in rentals.Where(r =>
                         r.ConsoleUnitId == consoleUnitId && r.Id != excludeRentalId))
            {
                // Rental selesai atau dibatalkan tidak lagi memblokir jadwal.
                if (existingRental.StatusRental is StatusRental.Completed or StatusRental.Cancelled)
                {
                    continue;
                }

                if (!existingRental.StartTime.HasValue || !existingRental.EndTime.HasValue)
                {
                    throw new InventoryNotAvailableException(
                        $"Ketersediaan console unit tidak dapat dipastikan karena jadwal rental {existingRental.RentalCode} belum lengkap.");
                }

                // Jadwal bentrok jika start A < end B dan start B < end A.
                var isOverlapping =
                    targetStart < existingRental.EndTime.Value &&
                    existingRental.StartTime.Value < targetEnd;

                if (isOverlapping)
                {
                    throw new InventoryNotAvailableException(
                        $"Console unit sudah dipesan pada rentang waktu tersebut oleh rental {existingRental.RentalCode}.");
                }
            }
        }

        private static void ValidateRequest(RentalRequest request)
        {
            if (request.ConsoleUnitId <= 0)
            {
                throw new ArgumentException("ConsoleUnitId harus lebih besar dari 0.");
            }

            if (request.StartTime == default)
            {
                throw new ArgumentException("StartTime wajib diisi dengan tanggal dan waktu yang valid.");
            }

            if (request.StartTime <= DateTimeOffset.UtcNow)
            {
                throw new ArgumentException(
                    "StartTime harus berada di masa depan. Kirim tanggal dan waktu ISO 8601 dengan zona waktu, misalnya 2026-11-21T14:00:00+07:00.");
            }

            if (request.EndTime == default)
            {
                throw new ArgumentException("EndTime wajib diisi dengan tanggal dan waktu yang valid.");
            }

            if (request.EndTime <= request.StartTime)
            {
                throw new InvalidRentalDurationException("EndTime harus lebih besar daripada StartTime.");
            }

            try
            {
                _ = (request.EndTime.UtcDateTime - request.StartTime.UtcDateTime).Ticks;
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new InvalidRentalDurationException("Rentang waktu Rental tidak valid.");
            }
        }

        private static string GenerateRentalCode()
        {
            var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            return $"RNT-{DateTime.UtcNow:yyyyMMddHHmmss}-{suffix}";
        }

        private static RentalResponse ToResponse(Rental rental)
        {
            return new RentalResponse
            {
                Id = rental.Id,
                RentalCode = rental.RentalCode,
                UserId = rental.UserId,
                ConsoleUnitId = rental.ConsoleUnitId,
                StartTime = rental.StartTime,
                EndTime = rental.EndTime,
                DurationHour = rental.DurationHour,
                ConsoleRate = rental.ConsoleRate,
                TotalAmount = rental.TotalAmount,
                StatusRental = rental.StatusRental
            };
        }
    }
}
