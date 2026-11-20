using RentalPS_BE.DTOs;
using RentalPS_BE.Repositories;
using RentalPSMiniProject.Models;
using Microsoft.Extensions.Logging;

namespace RentalPS_BE.Services
{
    public class RoomService : IRoomService
    {
        private readonly ILogger<RoomService> _logger;
        private readonly IRepository<Room> _roomRepository;
        private readonly IRepository<RoomType> _roomTypeRepository;
        private readonly IRepository<RentalRoom> _rentalRoomRepository;
        private readonly IRepository<Rental> _rentalRepository;

        public RoomService(
            IRepository<Room> roomRepository,
            IRepository<RoomType> roomTypeRepository,
            IRepository<RentalRoom> rentalRoomRepository,
            IRepository<Rental> rentalRepository,
            ILogger<RoomService> logger)
        {
            _roomRepository = roomRepository;
            _roomTypeRepository = roomTypeRepository;
            _rentalRoomRepository = rentalRoomRepository;
            _rentalRepository = rentalRepository;
            _logger = logger;
        }

        public async Task<List<RoomResponse>> GetAllAsync()
        {
            var rooms = await _roomRepository.GetAllAsync();

            return rooms
                .Select(ToResponse)
                .ToList();
        }

        public async Task<RoomResponse?> GetByIdAsync(int id)
        {
            var room = await _roomRepository.GetByIdAsync(id);

            return room == null
                ? null
                : ToResponse(room);
        }

        public async Task<RoomResponse> CreateAsync(RoomRequest request)
        {
            await ValidateRequestAsync(request);

            var room = new Room
            {
                RoomTypeId = request.RoomTypeId,
                RoomNumber = request.RoomNumber.Trim(),
                Description = request.Description.Trim(),
                StatusRoom = request.StatusRoom
            };

            await _roomRepository.AddAsync(room);
            await _roomRepository.SaveChangesAsync();
            _logger.LogInformation("Room {RoomId} ({RoomNumber}) created with status {Status}.", room.Id, room.RoomNumber, room.StatusRoom);

            return ToResponse(room);
        }

        public async Task<RoomResponse?> UpdateAsync(
            int id,
            RoomRequest request)
        {
            var room = await _roomRepository.GetByIdAsync(id);

            if (room == null)
            {
                return null;
            }

            await ValidateRequestAsync(request, id);

            var rentalRoomHistory = (await _rentalRoomRepository.GetAllAsync())
                .Where(detail => detail.RoomId == id)
                .ToList();

            if (rentalRoomHistory.Count > 0 &&
                (room.RoomTypeId != request.RoomTypeId ||
                 !string.Equals(room.RoomNumber, request.RoomNumber.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException(
                    "RoomType dan RoomNumber tidak dapat diubah setelah ruangan memiliki histori rental. Buat ruangan baru untuk menjaga histori transaksi.");
            }

            if (request.StatusRoom != StatusRoom.Available)
            {
                await EnsureRoomHasNoOpenRentalAsync(id);
            }

            room.RoomTypeId = request.RoomTypeId;
            room.RoomNumber = request.RoomNumber.Trim();
            room.Description = request.Description.Trim();
            room.StatusRoom = request.StatusRoom;

            _roomRepository.Update(room);
            await _roomRepository.SaveChangesAsync();
            _logger.LogInformation("Room {RoomId} ({RoomNumber}) updated. Status: {Status}.", room.Id, room.RoomNumber, room.StatusRoom);

            return ToResponse(room);
        }

        public async Task<bool> DeactivateAsync(int id)
        {
            var room = await _roomRepository.GetByIdAsync(id);

            if (room == null)
            {
                return false;
            }

            await EnsureRoomHasNoOpenRentalAsync(id);

            room.StatusRoom = StatusRoom.Inactive;
            _roomRepository.Update(room);
            await _roomRepository.SaveChangesAsync();
            _logger.LogInformation("Room {RoomId} ({RoomNumber}) deactivated.", room.Id, room.RoomNumber);

            return true;
        }

        private async Task EnsureRoomHasNoOpenRentalAsync(int roomId)
        {
            var rentalRooms = await _rentalRoomRepository.GetAllAsync();
            var relatedRentalIds = rentalRooms
                .Where(detail => detail.RoomId == roomId)
                .Select(detail => detail.RentalId)
                .ToHashSet();

            var rentals = await _rentalRepository.GetAllAsync();
            var hasOpenRental = rentals.Any(rental =>
                relatedRentalIds.Contains(rental.Id) &&
                (rental.StatusRental is StatusRental.Pending or StatusRental.Active));

            if (hasOpenRental)
            {
                throw new ArgumentException(
                    "Ruangan tidak dapat diubah menjadi Maintenance/Inactive karena masih digunakan atau dipesan oleh rental Pending/Active.");
            }
        }

        private async Task ValidateRequestAsync(
            RoomRequest request,
            int? excludedId = null)
        {
            if (!Enum.IsDefined(typeof(StatusRoom), request.StatusRoom))
            {
                throw new ArgumentException("StatusRoom tidak valid.");
            }

            var roomType = await _roomTypeRepository.GetByIdAsync(request.RoomTypeId);
            if (roomType == null || !roomType.IsActive)
            {
                throw new ArgumentException
                    ("RoomTypeId tidak ditemukan atau tidak aktif");
            }

            var rooms = await _roomRepository.GetAllAsync();
            var roomNumber = request.RoomNumber.Trim();

            var duplicateExists = rooms.Any(room =>
            room.Id != excludedId
            && string.Equals(
                room.RoomNumber,
                roomNumber,
                StringComparison.OrdinalIgnoreCase));

            if (duplicateExists)
            {
                throw new ArgumentException(
                    "Nomor Ruangan ini sudah digunakan");
            }
        }

        private static RoomResponse ToResponse(Room room)
        {
            return new RoomResponse
            {
                Id = room.Id,
                RoomTypeId = room.RoomTypeId,
                RoomNumber = room.RoomNumber,
                Description = room.Description,
                StatusRoom = room.StatusRoom
            };
        }
    }
}
