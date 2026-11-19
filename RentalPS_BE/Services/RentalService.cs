using RentalPS_BE.DTOs;
using RentalPS_BE.Repositories;
using RentalPSMiniProject.Models;

namespace RentalPS_BE.Services
{
    public class RentalService : IRentalService
    {
        private readonly IRepository<Rental> _rentalRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<ConsoleUnit> _consoleUnitRepository;
        private readonly IRepository<ConsoleType> _consoleTypeRepository;
        private readonly IRepository<RentalRoom> _rentalRoomRepository;
        private readonly IRepository<RentalStick> _rentalStickRepository;
        private readonly IRepository<Payment> _paymentRepository;

        public RentalService(
            IRepository<Rental> rentalRepository,
            IRepository<User> userRepository,
            IRepository<ConsoleUnit> consoleUnitRepository,
            IRepository<ConsoleType> consoleTypeRepository,
            IRepository<RentalRoom> rentalRoomRepository,
            IRepository<RentalStick> rentalStickRepository,
            IRepository<Payment> paymentRepository)
        {
            _rentalRepository = rentalRepository;
            _userRepository = userRepository;
            _consoleUnitRepository = consoleUnitRepository;
            _consoleTypeRepository = consoleTypeRepository;
            _rentalRoomRepository = rentalRoomRepository;
            _rentalStickRepository = rentalStickRepository;
            _paymentRepository = paymentRepository;
        }

        public async Task<List<RentalResponse>> GetAllAsync()
        {
            var rentals = await _rentalRepository.GetAllAsync();

            return rentals
                .OrderByDescending(r => r.Id)
                .Select(ToResponse)
                .ToList();
        }

        public async Task<RentalResponse?> GetByIdAsync(int id)
        {
            var rental = await _rentalRepository.GetByIdAsync(id);

            return rental == null ? null : ToResponse(rental);
        }

        public async Task<RentalResponse> CreateAsync(RentalRequest request)
        {
            ValidateRequest(request);

            var (user, consoleUnit, consoleType, endTime) =
                await ValidateRentalReferencesAsync(request, excludeRentalId: null);

            var rental = new Rental
            {
                RentalCode = GenerateRentalCode(),
                UserId = user.Id,
                ConsoleUnitId = consoleUnit.Id,
                StartTime = request.StartTime,
                EndTime = endTime,
                DurationHour = request.DurationHour,
                // Simpan tarif konsol sebagai snapshot saat transaksi dibuat.
                ConsoleRate = consoleType.HourlyRate,
                // Pada tahap awal, total hanya mencakup biaya konsol.
                // RentalRoom dan RentalStick menambahkan komponen biaya melalui detail masing-masing.
                TotalAmount = consoleType.HourlyRate * request.DurationHour,
                StatusRental = StatusRental.Pending
            };

            await _rentalRepository.AddAsync(rental);
            await _rentalRepository.SaveChangesAsync();

            return ToResponse(rental);
        }

        public async Task<RentalResponse?> UpdateAsync(int id, RentalRequest request)
        {
            ValidateRequest(request);

            var rental = await _rentalRepository.GetByIdAsync(id);
            if (rental == null)
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
                    "Rental tidak dapat diubah karena sudah memiliki catatan pembayaran. Batalkan atau proses penyesuaian pembayaran terlebih dahulu.");
            }

            var (user, consoleUnit, consoleType, endTime) =
                await ValidateRentalReferencesAsync(request, excludeRentalId: rental.Id);

            rental.UserId = user.Id;
            rental.ConsoleUnitId = consoleUnit.Id;
            rental.StartTime = request.StartTime;
            rental.EndTime = endTime;
            rental.DurationHour = request.DurationHour;
            rental.ConsoleRate = consoleType.HourlyRate;

            // Pertahankan snapshot tarif detail, tetapi hitung ulang subtotal
            // apabila durasi rental berubah.
            var rentalRooms = await _rentalRoomRepository.GetAllAsync();
            var currentRentalRooms = rentalRooms.Where(rr => rr.RentalId == rental.Id).ToList();
            foreach (var rentalRoom in currentRentalRooms)
            {
                rentalRoom.SubTotal = rentalRoom.AdditionalHourlyRate * rental.DurationHour;
                _rentalRoomRepository.Update(rentalRoom);
            }

            var rentalSticks = await _rentalStickRepository.GetAllAsync();
            var currentRentalSticks = rentalSticks.Where(rs => rs.RentalId == rental.Id).ToList();
            foreach (var rentalStick in currentRentalSticks)
            {
                rentalStick.SubTotal = rentalStick.HourlyRate * rental.DurationHour;
                _rentalStickRepository.Update(rentalStick);
            }

            rental.TotalAmount =
                (rental.ConsoleRate * rental.DurationHour) +
                currentRentalRooms.Sum(rr => rr.SubTotal) +
                currentRentalSticks.Sum(rs => rs.SubTotal);

            _rentalRepository.Update(rental);
            // Semua repository menggunakan DbContext scoped yang sama,
            // sehingga satu SaveChanges menyimpan seluruh perubahan di atas.
            await _rentalRepository.SaveChangesAsync();

            return ToResponse(rental);
        }

        public async Task<bool> CancelAsync(int id)
        {
            var rental = await _rentalRepository.GetByIdAsync(id);
            if (rental == null)
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
                    "Rental memiliki pembayaran berhasil. Proses pengembalian dana terlebih dahulu sebelum membatalkan transaksi.");
            }

            // Rental tidak dihapus secara fisik agar histori transaksi tetap tersimpan.
            rental.StatusRental = StatusRental.Cancelled;
            _rentalRepository.Update(rental);
            await _rentalRepository.SaveChangesAsync();

            return true;
        }

        private async Task<(User User, ConsoleUnit ConsoleUnit, ConsoleType ConsoleType, DateTime EndTime)>
            ValidateRentalReferencesAsync(RentalRequest request, int? excludeRentalId)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                throw new ArgumentException("UserId tidak ditemukan.");
            }

            var consoleUnit = await _consoleUnitRepository.GetByIdAsync(request.ConsoleUnitId);
            if (consoleUnit == null)
            {
                throw new ArgumentException("ConsoleUnitId tidak ditemukan.");
            }

            if (consoleUnit.StatusConsole != StatusConsole.Available)
            {
                throw new ArgumentException(
                    "Console unit tidak tersedia atau sedang digunakan/maintenance/nonaktif.");
            }

            var consoleType = await _consoleTypeRepository.GetByIdAsync(consoleUnit.ConsoleTypeId);
            if (consoleType == null)
            {
                throw new ArgumentException("ConsoleType untuk unit tersebut tidak ditemukan.");
            }

            if (!consoleType.IsActive)
            {
                throw new ArgumentException("Jenis console tersebut sedang tidak aktif.");
            }

            var endTime = request.StartTime.AddHours((double)request.DurationHour);
            await EnsureConsoleUnitIsNotBookedAsync(
                request.ConsoleUnitId,
                request.StartTime,
                endTime,
                excludeRentalId);

            return (user, consoleUnit, consoleType, endTime);
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
                    throw new ArgumentException(
                        $"Ketersediaan console unit tidak dapat dipastikan karena jadwal rental {existingRental.RentalCode} belum lengkap.");
                }

                // Jadwal bentrok jika start A < end B dan start B < end A.
                var isOverlapping =
                    targetStart < existingRental.EndTime.Value &&
                    existingRental.StartTime.Value < targetEnd;

                if (isOverlapping)
                {
                    throw new ArgumentException(
                        $"Console unit sudah dipesan pada rentang waktu tersebut oleh rental {existingRental.RentalCode}.");
                }
            }
        }

        private static void ValidateRequest(RentalRequest request)
        {
            if (request.UserId <= 0)
            {
                throw new ArgumentException("UserId harus lebih besar dari 0.");
            }

            if (request.ConsoleUnitId <= 0)
            {
                throw new ArgumentException("ConsoleUnitId harus lebih besar dari 0.");
            }

            if (request.StartTime == default)
            {
                throw new ArgumentException("StartTime wajib diisi dengan tanggal dan waktu yang valid.");
            }

            if (request.DurationHour <= 0)
            {
                throw new ArgumentException("Durasi rental harus lebih besar dari 0.");
            }

            try
            {
                _ = request.StartTime.AddHours((double)request.DurationHour);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new ArgumentException("Kombinasi waktu mulai dan durasi rental tidak valid.");
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
