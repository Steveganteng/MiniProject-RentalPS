using RentalPS_BE.DTOs;
using RentalPS_BE.Repositories;
using RentalPSMiniProject.Models;
using Microsoft.Extensions.Logging;

namespace RentalPS_BE.Services
{
    public class RoomTypeService : IRoomTypeService
    {
        private readonly ILogger<RoomTypeService> _logger;
        private readonly IRepository<RoomType> _repository;

        public RoomTypeService(IRepository<RoomType> repository, ILogger<RoomTypeService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<RoomTypeResponse>> GetAllAsync()
        {
            var roomTypes = await _repository.GetAllAsync();

            return roomTypes
                .Select(ToResponse)
                .ToList();
        }

        public async Task<RoomTypeResponse?> GetByIdAsync(int id)
        {
            var roomType = await _repository.GetByIdAsync(id);

            return roomType == null
                ? null
                : ToResponse(roomType);
        }

        public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
        {
            var normalizedName = name.Trim();
            var roomTypes = await _repository.GetAllAsync();

            return roomTypes.Any(roomType =>
                (!excludeId.HasValue || roomType.Id != excludeId.Value) &&
                string.Equals(
                    roomType.Name.Trim(),
                    normalizedName,
                    StringComparison.OrdinalIgnoreCase));
        }

        public async Task<RoomTypeResponse> CreateAsync(RoomTypeRequest request)
        {
            var roomType = new RoomType
            {
                Name = request.Name.Trim(),
                AdditionalHourlyRate = request.AdditionalHourlyRate,
                Description = request.Description.Trim(),
                IsActive = request.IsActive
            };

            await _repository.AddAsync(roomType);
            await _repository.SaveChangesAsync();
            _logger.LogInformation("Room type {RoomTypeId} ({Name}) created. AdditionalHourlyRate: {HourlyRate}.", roomType.Id, roomType.Name, roomType.AdditionalHourlyRate);

            return ToResponse(roomType);
        }

        public async Task<RoomTypeResponse?> UpdateAsync(
            int id,
            RoomTypeRequest request)
        {
            var roomType = await _repository.GetByIdAsync(id);

            if (roomType == null)
            {
                return null;
            }

            roomType.Name = request.Name.Trim();
            roomType.AdditionalHourlyRate = request.AdditionalHourlyRate;
            roomType.Description = request.Description.Trim();
            roomType.IsActive = request.IsActive;

            _repository.Update(roomType);
            await _repository.SaveChangesAsync();
            _logger.LogInformation("Room type {RoomTypeId} ({Name}) updated.", roomType.Id, roomType.Name);

            return ToResponse(roomType);
        }

        public async Task<bool> DeactivateAsync(int id)
        {
            var roomType = await _repository.GetByIdAsync(id);

            if (roomType == null)
            {
                return false;
            }

            roomType.IsActive = false;
            await _repository.SaveChangesAsync();
            _logger.LogInformation("Room type {RoomTypeId} ({Name}) deactivated.", roomType.Id, roomType.Name);

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
