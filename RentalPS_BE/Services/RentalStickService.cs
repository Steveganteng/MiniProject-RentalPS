using RentalPS_BE.DTOs;
using RentalPS_BE.Repositories;
using RentalPSMiniProject.Models;

namespace RentalPS_BE.Services
{
    public class RentalStickService : IRentalStickService
    {
        private readonly IRepository<RentalStick> _rentalStickRepository;
        private readonly IRepository<Rental> _rentalRepository;
        private readonly IRepository<Stick> _stickRepository;
        private readonly IRepository<StickType> _stickTypeRepository;

        public RentalStickService(
            IRepository<RentalStick> rentalStickRepository,
            IRepository<Rental> rentalRepository,
            IRepository<Stick> stickRepository,
            IRepository<StickType> stickTypeRepository)
        {
            _rentalStickRepository = rentalStickRepository;
            _rentalRepository = rentalRepository;
            _stickRepository = stickRepository;
            _stickTypeRepository = stickTypeRepository;
        }

        public async Task<List<RentalStickResponse>> GetAllAsync()
        {
            var rentalSticks = await _rentalStickRepository.GetAllAsync();

            return rentalSticks
                .Select(ToResponse)
                .ToList();
        }

        public async Task<RentalStickResponse?> GetByIdAsync(int id)
        {
            var rentalStick = await _rentalStickRepository.GetByIdAsync(id);

            return rentalStick == null
                ? null
                : ToResponse(rentalStick);
        }

        public async Task<RentalStickResponse> CreateAsync(RentalStickRequest request)
        {
            var (rental, stick, stickType) = await ValidateAssignmentAsync(request);

            var existingRentalSticks = await _rentalStickRepository.GetAllAsync();

            // Sesuai unique index (RentalId, StickId), satu unit stick tidak boleh
            // dimasukkan dua kali ke detail transaksi rental yang sama.
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
                // Tarif disalin dari jenis stick sebagai snapshot transaksi.
                HourlyRate = stickType.AdditionalHourlyRate,
                SubTotal = stickType.AdditionalHourlyRate * rental.DurationHour
            };

            await _rentalStickRepository.AddAsync(rentalStick);
            await _rentalStickRepository.SaveChangesAsync();

            return ToResponse(rentalStick);
        }

        public async Task<RentalStickResponse?> UpdateAsync(int id, RentalStickRequest request)
        {
            var rentalStick = await _rentalStickRepository.GetByIdAsync(id);
            if (rentalStick == null)
            {
                return null;
            }

            var (rental, stick, stickType) = await ValidateAssignmentAsync(request);

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

            rentalStick.RentalId = rental.Id;
            rentalStick.StickId = stick.Id;
            rentalStick.HourlyRate = stickType.AdditionalHourlyRate;
            rentalStick.SubTotal = stickType.AdditionalHourlyRate * rental.DurationHour;

            _rentalStickRepository.Update(rentalStick);
            await _rentalStickRepository.SaveChangesAsync();

            return ToResponse(rentalStick);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var rentalStick = await _rentalStickRepository.GetByIdAsync(id);
            if (rentalStick == null)
            {
                return false;
            }

            var rental = await _rentalRepository.GetByIdAsync(rentalStick.RentalId);
            if (rental == null)
            {
                throw new ArgumentException("Transaksi rental yang terhubung tidak ditemukan.");
            }

            // Detail stick hanya boleh dilepas selama transaksi masih Pending.
            if (rental.StatusRental != StatusRental.Pending)
            {
                throw new ArgumentException(
                    "Detail stick hanya dapat dihapus ketika transaksi masih berstatus Pending.");
            }

            // Hanya record detail rental_sticks yang dihapus, bukan data inventaris Stick.
            _rentalStickRepository.Delete(rentalStick);
            await _rentalStickRepository.SaveChangesAsync();

            return true;
        }

        private async Task<(Rental Rental, Stick Stick, StickType StickType)> ValidateAssignmentAsync(
            RentalStickRequest request)
        {
            var rental = await _rentalRepository.GetByIdAsync(request.RentalId);
            if (rental == null)
            {
                throw new ArgumentException("RentalId tidak ditemukan.");
            }

            // Penambahan/perubahan detail hanya diperbolehkan sebelum rental dimulai.
            if (rental.StatusRental != StatusRental.Pending)
            {
                throw new ArgumentException(
                    "Stick hanya dapat ditambahkan atau diubah ketika transaksi masih berstatus Pending.");
            }

            if (rental.DurationHour <= 0)
            {
                throw new ArgumentException("Durasi rental harus lebih besar dari 0.");
            }

            if (!rental.StartTime.HasValue || !rental.EndTime.HasValue)
            {
                throw new ArgumentException(
                    "StartTime dan EndTime rental harus sudah diisi sebelum stick ditetapkan.");
            }

            if (rental.EndTime.Value <= rental.StartTime.Value)
            {
                throw new ArgumentException("EndTime harus lebih besar daripada StartTime.");
            }

            var stick = await _stickRepository.GetByIdAsync(request.StickId);
            if (stick == null)
            {
                throw new ArgumentException("StickId tidak ditemukan.");
            }

            if (stick.StatusStick != StatusStick.Available)
            {
                throw new ArgumentException(
                    "Stick tidak tersedia atau sedang digunakan/maintenance/nonaktif.");
            }

            var stickType = await _stickTypeRepository.GetByIdAsync(stick.StickTypeId);
            if (stickType == null)
            {
                throw new ArgumentException("StickType untuk stick tersebut tidak ditemukan.");
            }

            if (!stickType.IsActive)
            {
                throw new ArgumentException("Jenis stick tersebut sedang tidak aktif.");
            }

            return (rental, stick, stickType);
        }

        private async Task EnsureStickIsNotBookedAsync(
            Rental targetRental,
            int stickId,
            int? currentRentalStickId = null)
        {
            var rentalSticks = await _rentalStickRepository.GetAllAsync();
            var rentals = await _rentalRepository.GetAllAsync();
            var rentalsById = rentals.ToDictionary(r => r.Id);

            foreach (var existingDetail in rentalSticks.Where(rs =>
                         rs.StickId == stickId && rs.Id != currentRentalStickId))
            {
                if (!rentalsById.TryGetValue(existingDetail.RentalId, out var existingRental))
                {
                    continue;
                }

                // Rental yang sudah selesai/dibatalkan tidak lagi memblokir jadwal.
                if (existingRental.StatusRental is StatusRental.Completed or StatusRental.Cancelled)
                {
                    continue;
                }

                if (!existingRental.StartTime.HasValue || !existingRental.EndTime.HasValue)
                {
                    throw new ArgumentException(
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
                    throw new ArgumentException(
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
