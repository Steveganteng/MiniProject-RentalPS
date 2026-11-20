using RentalPS_BE.DTOs;
using RentalPS_BE.Repositories;
using RentalPSMiniProject.Models;
using RentalPS_BE.Exceptions;
using Microsoft.Extensions.Logging;

namespace RentalPS_BE.Services
{
    public class RentalStickService : IRentalStickService
    {
        private readonly ILogger<RentalStickService> _logger;
        private readonly IRepository<RentalStick> _rentalStickRepository;
        private readonly IRepository<Rental> _rentalRepository;
        private readonly IRepository<Stick> _stickRepository;
        private readonly IRepository<StickType> _stickTypeRepository;
        private readonly IRepository<ConsoleUnit> _consoleUnitRepository;
        private readonly IRepository<RentalRoom> _rentalRoomRepository;
        private readonly IRepository<Payment> _paymentRepository;
        private readonly IRentalPricingService _rentalPricingService;

        public RentalStickService(
            IRepository<RentalStick> rentalStickRepository,
            IRepository<Rental> rentalRepository,
            IRepository<Stick> stickRepository,
            IRepository<StickType> stickTypeRepository,
            IRepository<ConsoleUnit> consoleUnitRepository,
            IRepository<RentalRoom> rentalRoomRepository,
            IRepository<Payment> paymentRepository,
            IRentalPricingService rentalPricingService,
            ILogger<RentalStickService> logger)
        {
            _rentalStickRepository = rentalStickRepository;
            _rentalRepository = rentalRepository;
            _stickRepository = stickRepository;
            _stickTypeRepository = stickTypeRepository;
            _consoleUnitRepository = consoleUnitRepository;
            _rentalRoomRepository = rentalRoomRepository;
            _paymentRepository = paymentRepository;
            _rentalPricingService = rentalPricingService;
            _logger = logger;
        }

        public async Task<List<RentalStickResponse>> GetAllAsync(int userId, bool isAdmin)
        {
            var rentalSticks = await _rentalStickRepository.GetAllAsync();

            if (isAdmin)
            {
                return rentalSticks.Select(ToResponse).ToList();
            }

            var ownedRentalIds = (await _rentalRepository.GetAllAsync())
                .Where(rental => rental.UserId == userId)
                .Select(rental => rental.Id)
                .ToHashSet();

            return rentalSticks
                .Where(rentalStick => ownedRentalIds.Contains(rentalStick.RentalId))
                .Select(ToResponse)
                .ToList();
        }

        public async Task<RentalStickResponse?> GetByIdAsync(int id, int userId, bool isAdmin)
        {
            var rentalStick = await _rentalStickRepository.GetByIdAsync(id);
            if (rentalStick == null)
            {
                return null;
            }

            var rental = await _rentalRepository.GetByIdAsync(rentalStick.RentalId);
            if (rental == null || (!isAdmin && rental.UserId != userId))
            {
                return null;
            }

            return ToResponse(rentalStick);
        }

        public async Task<RentalStickResponse> CreateAsync(int userId, RentalStickRequest request)
        {
            var (rental, stick, stickType) = await ValidateAssignmentAsync(request, userId);

            var existingRentalSticks = await _rentalStickRepository.GetAllAsync();

            // Satu unit stick tidak boleh ditambahkan dua kali pada rental yang sama.
            if (existingRentalSticks.Any(rs =>
                    rs.RentalId == rental.Id && rs.StickId == stick.Id))
            {
                throw new ArgumentException(
                    "Stick ini sudah ditambahkan ke transaksi rental tersebut.");
            }

            await EnsureStickIsNotBookedAsync(rental, stick.Id);

            var rentalStick = new RentalStick
            {
                RentalId = rental.Id,
                StickId = stick.Id,
                // Tarif disimpan sebagai snapshot saat detail rental dibuat.
                HourlyRate = stickType.AdditionalHourlyRate,
                SubTotal = _rentalPricingService.CalculateStickSubtotal(
                    stickType.AdditionalHourlyRate, rental)
            };

            // Masukkan detail baru ke daftar perhitungan sebelum SaveChanges.
            // Dengan begitu perubahan detail dan TotalAmount tersimpan bersama.
            existingRentalSticks.Add(rentalStick);
            var rentalRooms = await _rentalRoomRepository.GetAllAsync();
            rental.TotalAmount = _rentalPricingService.CalculateTotal(
                rental, rentalRooms, existingRentalSticks);

            _rentalRepository.Update(rental);
            await _rentalStickRepository.AddAsync(rentalStick);
            await _rentalStickRepository.SaveChangesAsync();
            _logger.LogInformation("RentalStick {RentalStickId} added to rental {RentalId} by user {UserId}. Stick: {StickId}, SubTotal: {SubTotal}, RentalTotal: {RentalTotal}.", rentalStick.Id, rentalStick.RentalId, userId, rentalStick.StickId, rentalStick.SubTotal, rental.TotalAmount);

            return ToResponse(rentalStick);
        }

        public async Task<RentalStickResponse?> UpdateAsync(
            int id,
            int userId,
            RentalStickRequest request)
        {
            var rentalStick = await _rentalStickRepository.GetByIdAsync(id);
            if (rentalStick == null)
            {
                return null;
            }

            // Periksa kepemilikan berdasarkan rental yang saat ini menaungi detail.
            var currentRental = await _rentalRepository.GetByIdAsync(rentalStick.RentalId);
            if (currentRental == null || currentRental.UserId != userId)
            {
                return null;
            }

            if (currentRental.StatusRental != StatusRental.Pending)
            {
                throw new ArgumentException(
                    "Detail stick hanya dapat diubah ketika rental milik Anda masih berstatus Pending.");
            }

            // Detail stick tidak boleh dipindahkan ke rental lain melalui Update.
            if (request.RentalId != rentalStick.RentalId)
            {
                throw new ArgumentException(
                    "RentalId tidak boleh diubah ketika memperbarui detail stick.");
            }

            var (rental, stick, stickType) = await ValidateAssignmentAsync(request, userId);

            var existingRentalSticks = await _rentalStickRepository.GetAllAsync();
            if (existingRentalSticks.Any(rs =>
                    rs.Id != rentalStick.Id &&
                    rs.RentalId == rental.Id &&
                    rs.StickId == stick.Id))
            {
                throw new ArgumentException(
                    "Stick ini sudah terdaftar pada transaksi rental tersebut.");
            }

            await EnsureStickIsNotBookedAsync(rental, stick.Id, rentalStick.Id);

            rentalStick.StickId = stick.Id;
            rentalStick.HourlyRate = stickType.AdditionalHourlyRate;
            rentalStick.SubTotal = _rentalPricingService.CalculateStickSubtotal(
                stickType.AdditionalHourlyRate, rental);

            var rentalSticksForTotal = await _rentalStickRepository.GetAllAsync();
            rentalSticksForTotal.RemoveAll(rs => rs.Id == rentalStick.Id);
            rentalSticksForTotal.Add(rentalStick);
            var rentalRoomsForTotal = await _rentalRoomRepository.GetAllAsync();

            rental.TotalAmount = _rentalPricingService.CalculateTotal(
                rental, rentalRoomsForTotal, rentalSticksForTotal);

            _rentalRepository.Update(rental);
            _rentalStickRepository.Update(rentalStick);
            await _rentalStickRepository.SaveChangesAsync();
            _logger.LogInformation("RentalStick {RentalStickId} in rental {RentalId} updated by user {UserId}. Stick: {StickId}, SubTotal: {SubTotal}, RentalTotal: {RentalTotal}.", rentalStick.Id, rentalStick.RentalId, userId, rentalStick.StickId, rentalStick.SubTotal, rental.TotalAmount);

            return ToResponse(rentalStick);
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            var rentalStick = await _rentalStickRepository.GetByIdAsync(id);
            if (rentalStick == null)
            {
                return false;
            }

            var rental = await _rentalRepository.GetByIdAsync(rentalStick.RentalId);
            if (rental == null || rental.UserId != userId)
            {
                return false;
            }

            if (rental.StatusRental != StatusRental.Pending)
            {
                throw new ArgumentException(
                    "Detail stick hanya dapat dihapus ketika rental milik Anda masih berstatus Pending.");
            }

            await EnsureRentalHasNoPaymentsAsync(rental.Id);

            // Hitung ulang tanpa detail stik yang akan dihapus.
            var rentalSticks = await _rentalStickRepository.GetAllAsync();
            rentalSticks.RemoveAll(rs => rs.Id == rentalStick.Id);
            var rentalRooms = await _rentalRoomRepository.GetAllAsync();
            rental.TotalAmount = _rentalPricingService.CalculateTotal(
                rental, rentalRooms, rentalSticks);

            _rentalRepository.Update(rental);
            // Hanya detail rental_sticks yang dihapus, bukan data inventaris Stick.
            _rentalStickRepository.Delete(rentalStick);
            await _rentalStickRepository.SaveChangesAsync();
            _logger.LogInformation("RentalStick {RentalStickId} removed from rental {RentalId} by user {UserId}. New RentalTotal: {RentalTotal}.", id, rental.Id, userId, rental.TotalAmount);

            return true;
        }

        private async Task<(Rental Rental, Stick Stick, StickType StickType)> ValidateAssignmentAsync(
            RentalStickRequest request,
            int userId)
        {
            var rental = await _rentalRepository.GetByIdAsync(request.RentalId);
            if (rental == null || rental.UserId != userId)
            {
                throw new NotFoundException("Rental tidak ditemukan atau bukan milik Anda.");
            }

            if (rental.StatusRental != StatusRental.Pending)
            {
                throw new ArgumentException(
                    "Stick hanya dapat ditambahkan atau diubah ketika rental milik Anda masih berstatus Pending.");
            }

            await EnsureRentalHasNoPaymentsAsync(rental.Id);

            if (rental.DurationHour <= 0)
            {
                throw new InvalidRentalDurationException("Durasi rental harus lebih besar dari 0.");
            }

            if (!rental.StartTime.HasValue || !rental.EndTime.HasValue)
            {
                throw new InvalidRentalDurationException(
                    "StartTime dan EndTime rental harus sudah diisi sebelum stick ditetapkan.");
            }

            if (rental.EndTime.Value <= rental.StartTime.Value)
            {
                throw new InvalidRentalDurationException("EndTime harus lebih besar daripada StartTime.");
            }

            var stick = await _stickRepository.GetByIdAsync(request.StickId);
            if (stick == null)
            {
                throw new NotFoundException("StickId tidak ditemukan.");
            }

            // Stick berstatus In_Use masih dapat dipesan untuk jadwal mendatang,
            // selama EnsureStickIsNotBookedAsync membuktikan tidak ada bentrok waktu.
            if (stick.StatusStick is StatusStick.Maintenance or StatusStick.Inactive)
            {
                throw new InventoryNotAvailableException(
                    "Stick sedang Maintenance atau Inactive sehingga tidak dapat dipesan.");
            }

            var stickType = await _stickTypeRepository.GetByIdAsync(stick.StickTypeId);
            if (stickType == null)
            {
                throw new NotFoundException("StickType untuk stick tersebut tidak ditemukan.");
            }

            if (!stickType.IsActive)
            {
                throw new ArgumentException("Jenis stick tersebut sedang tidak aktif.");
            }

            // Harga stik mengikuti generasi konsol. Cegah stik PS3 dipasang
            // pada rental PS4/PS5, dan sebaliknya.
            var consoleUnit = await _consoleUnitRepository.GetByIdAsync(rental.ConsoleUnitId);
            if (consoleUnit == null)
            {
                throw new NotFoundException("Console Unit pada rental tidak ditemukan.");
            }

            if (stickType.ConsoleTypeId != consoleUnit.ConsoleTypeId)
            {
                throw new ArgumentException(
                    "Jenis stik harus sesuai dengan generasi PlayStation pada rental (PS3, PS4, atau PS5).");
            }

            return (rental, stick, stickType);
        }

        private async Task EnsureRentalHasNoPaymentsAsync(int rentalId)
        {
            var payments = await _paymentRepository.GetAllAsync();
            if (payments.Any(payment => payment.RentalId == rentalId))
            {
                throw new ArgumentException(
                    "Detail stik tidak dapat diubah karena rental sudah memiliki riwayat pembayaran. Perubahan detail akan mengubah nilai tagihan yang telah dibuat.");
            }
        }

        private async Task EnsureStickIsNotBookedAsync(
            Rental targetRental,
            int stickId,
            int? currentRentalStickId = null)
        {
            var rentalSticks = await _rentalStickRepository.GetAllAsync();
            var rentals = await _rentalRepository.GetAllAsync();
            var rentalsById = rentals.ToDictionary(rental => rental.Id);

            foreach (var existingDetail in rentalSticks.Where(rentalStick =>
                         rentalStick.StickId == stickId && rentalStick.Id != currentRentalStickId))
            {
                if (!rentalsById.TryGetValue(existingDetail.RentalId, out var existingRental))
                {
                    continue;
                }

                // Rental selesai atau dibatalkan tidak lagi memblokir jadwal.
                if (existingRental.StatusRental is StatusRental.Completed or StatusRental.Cancelled)
                {
                    continue;
                }

                if (!existingRental.StartTime.HasValue || !existingRental.EndTime.HasValue)
                {
                    throw new InventoryNotAvailableException(
                        $"Ketersediaan stick tidak dapat dipastikan karena jadwal rental {existingRental.RentalCode} belum lengkap.");
                }

                var targetStart = targetRental.StartTime!.Value;
                var targetEnd = targetRental.EndTime!.Value;
                var existingStart = existingRental.StartTime.Value;
                var existingEnd = existingRental.EndTime.Value;

                // Dua rentang waktu bentrok jika start A < end B dan start B < end A.
                var isOverlapping = targetStart < existingEnd && existingStart < targetEnd;

                if (isOverlapping)
                {
                    throw new InventoryNotAvailableException(
                        $"Stick sedang digunakan/dipesan pada rentang waktu tersebut oleh rental {existingRental.RentalCode}.");
                }
            }
        }

        private static RentalStickResponse ToResponse(RentalStick rentalStick)
        {
            return new RentalStickResponse
            {
                Id = rentalStick.Id,
                RentalId = rentalStick.RentalId,
                StickId = rentalStick.StickId,
                HourlyRate = rentalStick.HourlyRate,
                SubTotal = rentalStick.SubTotal
            };
        }
    }
}
