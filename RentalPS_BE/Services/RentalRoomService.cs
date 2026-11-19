using RentalPS_BE.DTOs;
using RentalPS_BE.Repositories;
using RentalPSMiniProject.Models;

namespace RentalPS_BE.Services
{
    public class RentalRoomService : IRentalRoomService
    {
        private readonly IRepository<RentalRoom> _rentalRoomRepository;
        private readonly IRepository<Rental> _rentalRepository;
        private readonly IRepository<Room> _roomRepository;
        private readonly IRepository<RoomType> _roomTypeRepository;

        public RentalRoomService(
            IRepository<RentalRoom> rentalRoomRepository,
            IRepository<Rental> rentalRepository,
            IRepository<Room> roomRepository,
            IRepository<RoomType> roomTypeRepository)
        {
            _rentalRoomRepository = rentalRoomRepository;
            _rentalRepository = rentalRepository;
            _roomRepository = roomRepository;
            _roomTypeRepository = roomTypeRepository;
        }

        public async Task<List<RentalRoomResponse>> GetAllAsync()
        {
            var rentalRooms = await _rentalRoomRepository.GetAllAsync();

            return rentalRooms
                .Select(ToResponse)
                .ToList();
        }

        public async Task<RentalRoomResponse?> GetByIdAsync(int id)
        {
            var rentalRoom = await _rentalRoomRepository.GetByIdAsync(id);

            return rentalRoom == null
                ? null
                : ToResponse(rentalRoom);
        }

        public async Task<RentalRoomResponse> CreateAsync(RentalRoomRequest request)
        {
            var (rental, room, roomType) = await ValidateAssignmentAsync(request);

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
                SubTotal = roomType.AdditionalHourlyRate * rental.DurationHour
            };

            await _rentalRoomRepository.AddAsync(rentalRoom);
            await _rentalRoomRepository.SaveChangesAsync();

            return ToResponse(rentalRoom);
        }

        public async Task<RentalRoomResponse?> UpdateAsync(int id, RentalRoomRequest request)
        {
            var rentalRoom = await _rentalRoomRepository.GetByIdAsync(id);
            if (rentalRoom == null)
            {
                return null;
            }

            var (rental, room, roomType) = await ValidateAssignmentAsync(request);

            var existingRentalRooms = await _rentalRoomRepository.GetAllAsync();
            if (existingRentalRooms.Any(rr =>
                    rr.Id != rentalRoom.Id && rr.RentalId == rental.Id))
            {
                throw new ArgumentException(
                    "Transaksi rental tujuan sudah memiliki detail ruangan lain.");
            }

            await EnsureRoomIsNotBookedAsync(rental, room.Id);

            rentalRoom.RentalId = rental.Id;
            rentalRoom.RoomId = room.Id;
            rentalRoom.Name = roomType.Name;
            rentalRoom.AdditionalHourlyRate = roomType.AdditionalHourlyRate;
            rentalRoom.SubTotal = roomType.AdditionalHourlyRate * rental.DurationHour;

            _rentalRoomRepository.Update(rentalRoom);
            await _rentalRoomRepository.SaveChangesAsync();

            return ToResponse(rentalRoom);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var rentalRoom = await _rentalRoomRepository.GetByIdAsync(id);
            if (rentalRoom == null)
            {
                return false;
            }

            var rental = await _rentalRepository.GetByIdAsync(rentalRoom.RentalId);
            if (rental == null)
            {
                throw new ArgumentException("Transaksi rental yang terhubung tidak ditemukan.");
            }

            // Detail hanya boleh dilepas selama transaksi masih Pending.
            if (rental.StatusRental != StatusRental.Pending)
            {
                throw new ArgumentException(
                    "Detail ruangan hanya dapat dihapus ketika transaksi masih berstatus Pending.");
            }

            // Yang dihapus hanya detail penugasan ruangan, bukan data Room-nya.
            _rentalRoomRepository.Delete(rentalRoom);
            await _rentalRoomRepository.SaveChangesAsync();

            return true;
        }

        private async Task<(Rental Rental, Room Room, RoomType RoomType)> ValidateAssignmentAsync(
            RentalRoomRequest request)
        {
            var rental = await _rentalRepository.GetByIdAsync(request.RentalId);
            if (rental == null)
            {
                throw new ArgumentException("RentalId tidak ditemukan.");
            }

            // Room hanya boleh ditambahkan/diubah sebelum rental dimulai.
            if (rental.StatusRental != StatusRental.Pending)
            {
                throw new ArgumentException(
                    "Ruangan hanya dapat ditambahkan atau diubah ketika transaksi masih berstatus Pending.");
            }

            if (rental.DurationHour <= 0)
            {
                throw new ArgumentException("Durasi rental harus lebih besar dari 0.");
            }

            if (!rental.StartTime.HasValue || !rental.EndTime.HasValue)
            {
                throw new ArgumentException(
                    "StartTime dan EndTime rental harus sudah diisi sebelum ruangan ditetapkan.");
            }

            if (rental.EndTime.Value <= rental.StartTime.Value)
            {
                throw new ArgumentException("EndTime harus lebih besar daripada StartTime.");
            }

            var room = await _roomRepository.GetByIdAsync(request.RoomId);
            if (room == null)
            {
                throw new ArgumentException("RoomId tidak ditemukan.");
            }

            if (room.StatusRoom != StatusRoom.Available)
            {
                throw new ArgumentException("Ruangan tidak tersedia atau sedang tidak aktif/maintenance.");
            }

            var roomType = await _roomTypeRepository.GetByIdAsync(room.RoomTypeId);
            if (roomType == null)
            {
                throw new ArgumentException("RoomType untuk ruangan tersebut tidak ditemukan.");
            }

            if (!roomType.IsActive)
            {
                throw new ArgumentException("Jenis ruangan tersebut sedang tidak aktif.");
            }

            return (rental, room, roomType);
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
                    throw new ArgumentException(
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
                    throw new ArgumentException(
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
