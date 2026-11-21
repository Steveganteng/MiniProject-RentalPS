using RentalPS_BE.DTOs;
using RentalPS_BE.Repositories;
using RentalPSMiniProject.Models;

namespace RentalPS_BE.Services
{
    public class RoomService : IRoomService
    {
        private readonly IRepository<Room> _roomRepository;

        public RoomService(IRepository<Room> roomRepository)
        {
            _roomRepository = roomRepository;
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

            if (room == null)
            {
                return null;
            }

            return ToResponse(room);
        }

        public async Task<RoomResponse> CreateAsync(RoomRequest request)
        {
            var room = new Room
            {
                RoomTypeId = request.RoomTypeId,
                RoomNumber = request.RoomNumber,
                Description = request.Description,
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

            room.RoomTypeId = request.RoomTypeId;
            room.RoomNumber = request.RoomNumber;
            room.Description = request.Description;
            room.StatusRoom = request.StatusRoom;

            _roomRepository.Update(room);
            await _roomRepository.SaveChangesAsync();

            return ToResponse(room);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var room = await _roomRepository.GetByIdAsync(id);

            if (room == null)
            {
                return false;
            }

            room.StatusRoom = StatusRoom.Inactive;

            _roomRepository.Update(room);
            await _roomRepository.SaveChangesAsync();

            return true;
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