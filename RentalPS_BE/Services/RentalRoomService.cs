using RentalPS_BE.DTOs;
using RentalPS_BE.Repositories;
using RentalPSMiniProject.Models;
using RentalPS_BE.Exceptions;
using Microsoft.Extensions.Logging;

namespace RentalPS_BE.Services
{
    public class RentalRoomService : IRentalRoomService
    {
        private readonly ILogger<RentalRoomService> _logger;
        private readonly IRepository<RentalRoom> _rentalRoomRepository;
        private readonly IRepository<Rental> _rentalRepository;
        private readonly IRepository<Room> _roomRepository;
        private readonly IRepository<RoomType> _roomTypeRepository;
        private readonly IRepository<RentalStick> _rentalStickRepository;
        private readonly IRepository<Payment> _paymentRepository;
        private readonly IRentalPricingService _rentalPricingService;

        public RentalRoomService(
            IRepository<RentalRoom> rentalRoomRepository,
            IRepository<Rental> rentalRepository,
            IRepository<Room> roomRepository,
            IRepository<RoomType> roomTypeRepository,
            IRepository<RentalStick> rentalStickRepository,
            IRepository<Payment> paymentRepository,
            IRentalPricingService rentalPricingService,
            ILogger<RentalRoomService> logger)
        {
            _rentalRoomRepository = rentalRoomRepository;
            _rentalRepository = rentalRepository;
            _roomRepository = roomRepository;
            _roomTypeRepository = roomTypeRepository;
            _rentalStickRepository = rentalStickRepository;
            _paymentRepository = paymentRepository;
            _rentalPricingService = rentalPricingService;
            _logger = logger;
        }

        public async Task<List<RentalRoomResponse>> GetAllAsync(int userId, bool isAdmin)
        {
            var rentalRooms = await _rentalRoomRepository.GetAllAsync();

            if (isAdmin)
            {
                return rentalRooms.Select(ToResponse).ToList();
            }

            var ownedRentalIds = (await _rentalRepository.GetAllAsync())
                .Where(rental => rental.UserId == userId)
                .Select(rental => rental.Id)
                .ToHashSet();

            return rentalRooms
                .Where(rentalRoom => ownedRentalIds.Contains(rentalRoom.RentalId))
                .Select(ToResponse)
                .ToList();
        }

        public async Task<RentalRoomResponse?> GetByIdAsync(int id, int userId, bool isAdmin)
        {
            var rentalRoom = await _rentalRoomRepository.GetByIdAsync(id);
            if (rentalRoom == null)
            {
                return null;
            }

            var rental = await _rentalRepository.GetByIdAsync(rentalRoom.RentalId);
            if (rental == null || (!isAdmin && rental.UserId != userId))
            {
                return null;
            }

            return ToResponse(rentalRoom);
        }

        public async Task<RentalRoomResponse> CreateAsync(int userId, RentalRoomRequest request)
        {
            var (rental, room, roomType) = await ValidateAssignmentAsync(request, userId);

            var existingRentalRooms = await _rentalRoomRepository.GetAllAsync();
            if (existingRentalRooms.Any(rr => rr.RentalId == rental.Id))
            {
                throw new ArgumentException(
                    "Transaksi rental ini sudah memiliki ruangan. Satu rental hanya boleh memiliki satu RentalRoom.");
            }

            await EnsureRoomIsNotBookedAsync(rental, room.Id);

            var rentalRoom = new RentalRoom
            {
                RentalId = rental.Id,
                RoomId = room.Id,
                // Simpan snapshot nama dan tarif pada saat transaksi dibuat.
                Name = roomType.Name,
                AdditionalHourlyRate = roomType.AdditionalHourlyRate,
                SubTotal = _rentalPricingService.CalculateRoomSubtotal(
                    roomType.AdditionalHourlyRate, roomType.Name, rental)
            };

            // Masukkan detail baru ke daftar perhitungan sebelum SaveChanges.
            // Dengan begitu perubahan detail dan TotalAmount tersimpan bersama.
            existingRentalRooms.Add(rentalRoom);
            var rentalSticks = await _rentalStickRepository.GetAllAsync();
            rental.TotalAmount = _rentalPricingService.CalculateTotal(
                rental, existingRentalRooms, rentalSticks);

            _rentalRepository.Update(rental);
            await _rentalRoomRepository.AddAsync(rentalRoom);
            await _rentalRoomRepository.SaveChangesAsync();
            _logger.LogInformation("RentalRoom {RentalRoomId} added to rental {RentalId} by user {UserId}. Room: {RoomId}, SubTotal: {SubTotal}, RentalTotal: {RentalTotal}.", rentalRoom.Id, rentalRoom.RentalId, userId, rentalRoom.RoomId, rentalRoom.SubTotal, rental.TotalAmount);

            return ToResponse(rentalRoom);
        }

        public async Task<RentalRoomResponse?> UpdateAsync(
            int id,
            int userId,
            RentalRoomRequest request)
        {
            var rentalRoom = await _rentalRoomRepository.GetByIdAsync(id);
            if (rentalRoom == null)
            {
                return null;
            }

            var currentRental = await _rentalRepository.GetByIdAsync(rentalRoom.RentalId);
            if (currentRental == null || currentRental.UserId != userId)
            {
                return null;
            }

            if (currentRental.StatusRental != StatusRental.Pending)
            {
                throw new ArgumentException(
                    "Detail ruangan hanya dapat diubah ketika rental milik Anda masih berstatus Pending.");
            }

            // Detail ruangan tidak boleh dipindahkan ke transaksi rental lain.
            if (request.RentalId != rentalRoom.RentalId)
            {
                throw new ArgumentException(
                    "RentalId tidak boleh diubah ketika memperbarui detail ruangan.");
            }

            var (rental, room, roomType) = await ValidateAssignmentAsync(request, userId);

            var existingRentalRooms = await _rentalRoomRepository.GetAllAsync();
            if (existingRentalRooms.Any(rr =>
                    rr.Id != rentalRoom.Id && rr.RentalId == rental.Id))
            {
                throw new ArgumentException(
                    "Transaksi rental tujuan sudah memiliki detail ruangan lain.");
            }

            await EnsureRoomIsNotBookedAsync(rental, room.Id);

            rentalRoom.RoomId = room.Id;
            rentalRoom.Name = roomType.Name;
            rentalRoom.AdditionalHourlyRate = roomType.AdditionalHourlyRate;
            rentalRoom.SubTotal = _rentalPricingService.CalculateRoomSubtotal(
                roomType.AdditionalHourlyRate, roomType.Name, rental);

            var rentalRoomsForTotal = await _rentalRoomRepository.GetAllAsync();
            rentalRoomsForTotal.RemoveAll(rr => rr.Id == rentalRoom.Id);
            rentalRoomsForTotal.Add(rentalRoom);
            var rentalSticksForTotal = await _rentalStickRepository.GetAllAsync();

            rental.TotalAmount = _rentalPricingService.CalculateTotal(
                rental, rentalRoomsForTotal, rentalSticksForTotal);

            _rentalRepository.Update(rental);
            _rentalRoomRepository.Update(rentalRoom);
            await _rentalRoomRepository.SaveChangesAsync();
            _logger.LogInformation("RentalRoom {RentalRoomId} in rental {RentalId} updated by user {UserId}. Room: {RoomId}, SubTotal: {SubTotal}, RentalTotal: {RentalTotal}.", rentalRoom.Id, rentalRoom.RentalId, userId, rentalRoom.RoomId, rentalRoom.SubTotal, rental.TotalAmount);

            return ToResponse(rentalRoom);
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            var rentalRoom = await _rentalRoomRepository.GetByIdAsync(id);
            if (rentalRoom == null)
            {
                return false;
            }

            var rental = await _rentalRepository.GetByIdAsync(rentalRoom.RentalId);
            if (rental == null || rental.UserId != userId)
            {
                return false;
            }

            if (rental.StatusRental != StatusRental.Pending)
            {
                throw new ArgumentException(
                    "Detail ruangan hanya dapat dihapus ketika rental milik Anda masih berstatus Pending.");
            }

            await EnsureRentalHasNoPaymentsAsync(rental.Id);

            // Hitung ulang tanpa detail ruangan yang akan dihapus.
            var rentalRooms = await _rentalRoomRepository.GetAllAsync();
            rentalRooms.RemoveAll(rr => rr.Id == rentalRoom.Id);
            var rentalSticks = await _rentalStickRepository.GetAllAsync();
            rental.TotalAmount = _rentalPricingService.CalculateTotal(
                rental, rentalRooms, rentalSticks);

            _rentalRepository.Update(rental);
            // Yang dihapus hanya detail penugasan ruangan, bukan data Room-nya.
            _rentalRoomRepository.Delete(rentalRoom);
            await _rentalRoomRepository.SaveChangesAsync();
            _logger.LogInformation("RentalRoom {RentalRoomId} removed from rental {RentalId} by user {UserId}. New RentalTotal: {RentalTotal}.", id, rental.Id, userId, rental.TotalAmount);

            return true;
        }

        private async Task<(Rental Rental, Room Room, RoomType RoomType)> ValidateAssignmentAsync(
            RentalRoomRequest request,
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
                    "Ruangan hanya dapat ditambahkan atau diubah ketika rental milik Anda masih berstatus Pending.");
            }

            await EnsureRentalHasNoPaymentsAsync(rental.Id);

            if (rental.DurationHour <= 0)
            {
                throw new InvalidRentalDurationException("Durasi rental harus lebih besar dari 0.");
            }

            if (!rental.StartTime.HasValue || !rental.EndTime.HasValue)
            {
                throw new InvalidRentalDurationException(
                    "StartTime dan EndTime rental harus sudah diisi sebelum ruangan ditetapkan.");
            }

            if (rental.EndTime.Value <= rental.StartTime.Value)
            {
                throw new InvalidRentalDurationException("EndTime harus lebih besar daripada StartTime.");
            }

            var room = await _roomRepository.GetByIdAsync(request.RoomId);
            if (room == null)
            {
                throw new NotFoundException("RoomId tidak ditemukan.");
            }

            if (room.StatusRoom != StatusRoom.Available)
            {
                throw new InventoryNotAvailableException("Ruangan tidak tersedia atau sedang tidak aktif/maintenance.");
            }

            var roomType = await _roomTypeRepository.GetByIdAsync(room.RoomTypeId);
            if (roomType == null)
            {
                throw new NotFoundException("RoomType untuk ruangan tersebut tidak ditemukan.");
            }

            if (!roomType.IsActive)
            {
                throw new ArgumentException("Jenis ruangan tersebut sedang tidak aktif.");
            }

            return (rental, room, roomType);
        }

        private async Task EnsureRentalHasNoPaymentsAsync(int rentalId)
        {
            var payments = await _paymentRepository.GetAllAsync();
            if (payments.Any(payment => payment.RentalId == rentalId))
            {
                throw new ArgumentException(
                    "Detail ruangan tidak dapat diubah karena rental sudah memiliki riwayat pembayaran. Perubahan detail akan mengubah nilai tagihan yang telah dibuat.");
            }
        }

        private async Task EnsureRoomIsNotBookedAsync(Rental targetRental, int roomId)
        {
            var rentalRooms = await _rentalRoomRepository.GetAllAsync();
            var rentals = await _rentalRepository.GetAllAsync();
            var rentalsById = rentals.ToDictionary(r => r.Id);

            foreach (var existingDetail in rentalRooms.Where(rr =>
                         rr.RoomId == roomId && rr.RentalId != targetRental.Id))
            {
                if (!rentalsById.TryGetValue(existingDetail.RentalId, out var existingRental))
                {
                    continue;
                }

                // Transaksi yang sudah selesai atau dibatalkan tidak memblokir jadwal.
                if (existingRental.StatusRental is StatusRental.Completed or StatusRental.Cancelled)
                {
                    continue;
                }

                // Jika jadwal transaksi lama belum lengkap, jangan mengambil risiko
                // mengalokasikan ruangan yang sama tanpa bisa memeriksa bentrok waktunya.
                if (!existingRental.StartTime.HasValue || !existingRental.EndTime.HasValue)
                {
                    throw new InventoryNotAvailableException(
                        $"Ketersediaan ruangan tidak dapat dipastikan karena jadwal rental {existingRental.RentalCode} belum lengkap.");
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
                        $"Ruangan sedang digunakan/dipesan pada rentang waktu tersebut oleh rental {existingRental.RentalCode}.");
                }
            }
        }

        private static RentalRoomResponse ToResponse(RentalRoom rentalRoom)
        {
            return new RentalRoomResponse
            {
                Id = rentalRoom.Id,
                RentalId = rentalRoom.RentalId,
                RoomId = rentalRoom.RoomId,
                Name = rentalRoom.Name,
                AdditionalHourlyRate = rentalRoom.AdditionalHourlyRate,
                SubTotal = rentalRoom.SubTotal
            };
        }
    }
}
