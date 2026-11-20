using RentalPS_BE.DTOs;
using RentalPS_BE.Repositories;
using RentalPSMiniProject.Models;
using Microsoft.Extensions.Logging;

namespace RentalPS_BE.Services
{
    public class StickService : IStickService
    {
        private readonly ILogger<StickService> _logger;
        private readonly IRepository<Stick> _stickRepository;
        private readonly IRepository<StickType> _stickTypeRepository;
        private readonly IRepository<RentalStick> _rentalStickRepository;
        private readonly IRepository<Rental> _rentalRepository;

        public StickService(
            IRepository<Stick> stickRepository,
            IRepository<StickType> stickTypeRepository,
            IRepository<RentalStick> rentalStickRepository,
            IRepository<Rental> rentalRepository,
            ILogger<StickService> logger)
        {
            _stickRepository = stickRepository;
            _stickTypeRepository = stickTypeRepository;
            _rentalStickRepository = rentalStickRepository;
            _rentalRepository = rentalRepository;
            _logger = logger;
        }

        public async Task<List<StickResponse>> GetAllAsync()
        {
            var sticks = await _stickRepository.GetAllAsync();
            return sticks.Select(ToResponse).ToList();
        }

        public async Task<StickResponse?> GetByIdAsync(int id)
        {
            var stick = await _stickRepository.GetByIdAsync(id);
            return stick == null ? null : ToResponse(stick);
        }

        public async Task<StickResponse> CreateAsync(StickRequest request)
        {
            ValidateStatus(request.StatusStick);
            var stickType = await _stickTypeRepository.GetByIdAsync(request.StickTypeId);
            if (stickType == null || !stickType.IsActive)
            {
                throw new ArgumentException("StickTypeId tidak ditemukan atau jenis stik sedang tidak aktif.");
            }

            if (string.IsNullOrWhiteSpace(request.StickCode))
            {
                throw new ArgumentException("StickCode wajib diisi.");
            }

            var normalizedCode = request.StickCode.Trim();
            var sticks = await _stickRepository.GetAllAsync();
            if (sticks.Any(item => string.Equals(
                    item.StickCode, normalizedCode, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"StickCode '{normalizedCode}' sudah digunakan.");
            }

            var stick = new Stick
            {
                StickTypeId = request.StickTypeId,
                StickCode = normalizedCode,
                StatusStick = request.StatusStick
            };

            await _stickRepository.AddAsync(stick);
            await _stickRepository.SaveChangesAsync();
            _logger.LogInformation("Stick {StickId} ({StickCode}) created with status {Status}.", stick.Id, stick.StickCode, stick.StatusStick);
            return ToResponse(stick);
        }

        public async Task<StickResponse?> UpdateAsync(int id, StickRequest request)
        {
            ValidateStatus(request.StatusStick);
            var stick = await _stickRepository.GetByIdAsync(id);
            if (stick == null)
            {
                return null;
            }

            var stickType = await _stickTypeRepository.GetByIdAsync(request.StickTypeId);
            if (stickType == null || !stickType.IsActive)
            {
                throw new ArgumentException("StickTypeId tidak ditemukan atau jenis stik sedang tidak aktif.");
            }

            if (string.IsNullOrWhiteSpace(request.StickCode))
            {
                throw new ArgumentException("StickCode wajib diisi.");
            }

            var normalizedCode = request.StickCode.Trim();
            var allSticks = await _stickRepository.GetAllAsync();
            if (allSticks.Any(item => item.Id != id && string.Equals(
                    item.StickCode, normalizedCode, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"StickCode '{normalizedCode}' sudah digunakan.");
            }

            var details = await _rentalStickRepository.GetAllAsync();
            var history = details.Where(detail => detail.StickId == id).ToList();
            var relatedRentalIds = history.Select(detail => detail.RentalId).ToHashSet();
            var rentals = await _rentalRepository.GetAllAsync();
            var relatedRentals = rentals.Where(rental => relatedRentalIds.Contains(rental.Id)).ToList();

            if (relatedRentals.Any(rental => rental.StatusRental == StatusRental.Active))
            {
                throw new ArgumentException(
                    "Stik sedang digunakan oleh rental Active. Selesaikan rental terlebih dahulu sebelum mengubah inventaris.");
            }

            if (history.Count > 0 &&
                (stick.StickTypeId != request.StickTypeId ||
                 !string.Equals(stick.StickCode, normalizedCode, StringComparison.Ordinal)))
            {
                throw new ArgumentException(
                    "StickType dan StickCode tidak dapat diubah setelah stik memiliki histori rental. Buat inventaris stik baru untuk menjaga histori transaksi.");
            }

            if (request.StatusStick != StatusStick.Available &&
                relatedRentals.Any(rental => rental.StatusRental == StatusRental.Pending))
            {
                throw new ArgumentException(
                    "Stik tidak dapat diubah menjadi Maintenance/Inactive karena masih dipesan oleh rental Pending.");
            }

            stick.StickTypeId = request.StickTypeId;
            stick.StickCode = normalizedCode;
            stick.StatusStick = request.StatusStick;

            _stickRepository.Update(stick);
            await _stickRepository.SaveChangesAsync();
            _logger.LogInformation("Stick {StickId} ({StickCode}) updated. Status: {Status}.", stick.Id, stick.StickCode, stick.StatusStick);
            return ToResponse(stick);
        }

        public async Task<bool> DeactivateAsync(int id)
        {
            var stick = await _stickRepository.GetByIdAsync(id);
            if (stick == null)
            {
                return false;
            }

            await EnsureStickHasNoOpenRentalAsync(id);
            stick.StatusStick = StatusStick.Inactive;
            _stickRepository.Update(stick);
            await _stickRepository.SaveChangesAsync();
            _logger.LogInformation("Stick {StickId} ({StickCode}) deactivated.", stick.Id, stick.StickCode);
            return true;
        }

        private async Task EnsureStickHasNoOpenRentalAsync(int stickId)
        {
            var details = await _rentalStickRepository.GetAllAsync();
            var relatedRentalIds = details
                .Where(detail => detail.StickId == stickId)
                .Select(detail => detail.RentalId)
                .ToHashSet();

            var rentals = await _rentalRepository.GetAllAsync();
            var hasOpenRental = rentals.Any(rental =>
                relatedRentalIds.Contains(rental.Id) &&
                (rental.StatusRental is StatusRental.Pending or StatusRental.Active));

            if (hasOpenRental)
            {
                throw new ArgumentException(
                    "Stik tidak dapat dinonaktifkan karena masih digunakan atau dipesan oleh rental Pending/Active.");
            }
        }

        private static void ValidateStatus(StatusStick status)
        {
            if (!Enum.IsDefined(typeof(StatusStick), status))
            {
                throw new ArgumentException("StatusStick tidak valid.");
            }

            if (status == StatusStick.In_Use)
            {
                throw new ArgumentException(
                    "Status In_Use diatur otomatis ketika Admin mengubah StatusRental menjadi Active.");
            }
        }

        private static StickResponse ToResponse(Stick stick)
        {
            return new StickResponse
            {
                Id = stick.Id,
                StickTypeId = stick.StickTypeId,
                StickCode = stick.StickCode,
                StatusStick = stick.StatusStick
            };
        }
    }
}
