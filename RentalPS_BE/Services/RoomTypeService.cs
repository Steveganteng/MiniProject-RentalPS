using RentalPS_BE.DTOs;
using RentalPS_BE.Repositories;
using RentalPSMiniProject.Models;

namespace RentalPS_BE.Services
{
    public class RoomTypeService : IRoomTypeService
    {
        private readonly IRepository<RoomType> _roomTypeRepository;

        public RoomTypeService(
            IRepository<RoomType> roomTypeRepository)
        {
            _roomTypeRepository = roomTypeRepository;
        }

        public async Task<List<RoomTypeResponse>> GetAllAsync()
        {
            var roomTypes =
                await _roomTypeRepository.GetAllAsync();

            return roomTypes
                .Select(ToResponse)
                .ToList();
        }

        public async Task<RoomTypeResponse?> GetByIdAsync(int id)
        {
            var roomType =
                await _roomTypeRepository.GetByIdAsync(id);

            if (roomType == null)
            {
                return null;
            }

            return ToResponse(roomType);
        }

        public async Task<RoomTypeResponse> CreateAsync(
            RoomTypeRequest request)
        {
            var roomType = new RoomType
            {
                Name = request.Name,
                AdditionalHourlyRate = request.AdditionalHourlyRate,
                Description = request.Description,
                IsActive = request.IsActive
            };

            await _roomTypeRepository.AddAsync(roomType);
            await _roomTypeRepository.SaveChangesAsync();

            return ToResponse(roomType);
        }

        public async Task<RoomTypeResponse?> UpdateAsync(
            int id,
            RoomTypeRequest request)
        {
            var roomType =
                await _roomTypeRepository.GetByIdAsync(id);

            if (roomType == null)
            {
                return null;
            }

            roomType.Name = request.Name;
            roomType.AdditionalHourlyRate = request.AdditionalHourlyRate;
            roomType.Description = request.Description;
            roomType.IsActive = request.IsActive;

            _roomTypeRepository.Update(roomType);
            await _roomTypeRepository.SaveChangesAsync();

            return ToResponse(roomType);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var roomType =
                await _roomTypeRepository.GetByIdAsync(id);

            if (roomType == null)
            {
                return false;
            }

            // Soft delete karena RoomType memiliki field IsActive.
            roomType.IsActive = false;

            _roomTypeRepository.Update(roomType);
            await _roomTypeRepository.SaveChangesAsync();

            return true;
        }

        private static RoomTypeResponse ToResponse(RoomType roomType)
        {
            return new RoomTypeResponse
            {
                Id = roomType.Id,
                Name = roomType.Name,
                AdditionalHourlyRate = roomType.AdditionalHourlyRate,
                Description = roomType.Description,
                IsActive = roomType.IsActive
            };
        }
    }
}