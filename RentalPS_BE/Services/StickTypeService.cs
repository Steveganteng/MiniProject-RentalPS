using RentalPS_BE.DTOs;
using RentalPS_BE.Repositories;
using RentalPSMiniProject.Models;
using Microsoft.Extensions.Logging;

namespace RentalPS_BE.Services
{
    public class StickTypeService : IStickTypeService
    {
        private readonly ILogger<StickTypeService> _logger;
        private readonly IRepository<StickType> _repository;

        public StickTypeService(IRepository<StickType> repository, ILogger<StickTypeService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<StickTypeResponse>> GetAllAsync()
        {
            var stickTypes = await _repository.GetAllAsync();

            return stickTypes
                .Select(ToResponse)
                .ToList();
        }

        public async Task<StickTypeResponse?> GetByIdAsync(int id)
        {
            var stickType = await _repository.GetByIdAsync(id);

            return stickType == null
                ? null
                : ToResponse(stickType);
        }

        public async Task<StickTypeResponse> CreateAsync(StickTypeRequest request)
        {
            var stickType = new StickType
            {
                ConsoleTypeId = request.ConsoleTypeId,
                Name = request.Name.Trim(),
                AdditionalHourlyRate = request.AdditionalHourlyRate,
                IsActive = request.IsActive
            };

            await _repository.AddAsync(stickType);
            await _repository.SaveChangesAsync();
            _logger.LogInformation("Stick type {StickTypeId} ({Name}) created. AdditionalHourlyRate: {HourlyRate}.", stickType.Id, stickType.Name, stickType.AdditionalHourlyRate);

            return ToResponse(stickType);
        }

        public async Task<StickTypeResponse?> UpdateAsync(
            int id,
            StickTypeRequest request)
        {
            var stickType = await _repository.GetByIdAsync(id);

            if (stickType == null)
            {
                return null;
            }

            stickType.ConsoleTypeId = request.ConsoleTypeId;
            stickType.Name = request.Name.Trim();
            stickType.AdditionalHourlyRate = request.AdditionalHourlyRate;
            stickType.IsActive = request.IsActive;

            _repository.Update(stickType);
            await _repository.SaveChangesAsync();
            _logger.LogInformation("Stick type {StickTypeId} ({Name}) updated.", stickType.Id, stickType.Name);

            return ToResponse(stickType);
        }

        public async Task<bool> DeactivateAsync(int id)
        {
            var stickType = await _repository.GetByIdAsync(id);

            if (stickType == null)
            {
                return false;
            }

            stickType.IsActive = false;
            await _repository.SaveChangesAsync();
            _logger.LogInformation("Stick type {StickTypeId} ({Name}) deactivated.", stickType.Id, stickType.Name);

            return true;
        }

        private static StickTypeResponse ToResponse(StickType stickType)
        {
            return new StickTypeResponse
            {
                Id = stickType.Id,
                ConsoleTypeId = stickType.ConsoleTypeId,
                Name = stickType.Name,
                AdditionalHourlyRate = stickType.AdditionalHourlyRate,
                IsActive = stickType.IsActive
            };
        }
    }
}
