using RentalPS_BE.DTOs;
using RentalPS_BE.Repositories;
using RentalPSMiniProject.Models;

namespace RentalPS_BE.Services
{
    public class DuplicateRoomNumberException : Exception
    {
        public DuplicateRoomNumberException(string message) : base(message)
        {
        }
    }
    public class RoomService : IRoomService
    {
        private readonly IRepository<Room> _roomRepository;
        private readonly IRepository<RoomType> _roomTypeRepository;

        public RoomService(
            IRepository<Room> roomRepository,
            IRepository<RoomType> roomTypeRepository)
        {
            _roomRepository = roomRepository;
            _roomTypeRepository = roomTypeRepository;
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

            room.RoomTypeId = request.RoomTypeId;
            room.RoomNumber = request.RoomNumber.Trim();
            room.Description = request.Description.Trim();
            room.StatusRoom = request.StatusRoom;

            _roomRepository.Update(room);
            await _roomRepository.SaveChangesAsync();

            return ToResponse(room);
        }

        public async Task<bool> DeactivateAsync(int id)
        {
            var room = await _roomRepository.GetByIdAsync(id);

            if (room == null)
            {
                return false;
            }

            room.StatusRoom = StatusRoom.Inactive;
            await _roomRepository.SaveChangesAsync();

            return true;
        }

        private async Task ValidateRequestAsync(
            RoomRequest request,
            int? excludedId = null)
        {
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
                throw new DuplicateRoomNumberException(
                    "Nomor ruangan ini sudah digunakan.");
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
