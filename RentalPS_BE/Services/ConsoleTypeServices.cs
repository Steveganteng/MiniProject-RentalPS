using RentalPS_BE.DTOs;
using RentalPS_BE.Repositories;
using RentalPSMiniProject.Models;
using Microsoft.Extensions.Logging;

namespace RentalPS_BE.Services
{
    public class ConsoleTypeService : IConsoleTypeService
    {
        private readonly ILogger<ConsoleTypeService> _logger;
        private readonly IRepository<ConsoleType> _repository;

        public ConsoleTypeService(IRepository<ConsoleType> repository, ILogger<ConsoleTypeService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<ConsoleTypeResponse>> GetAllAsync()
        {
            var consoleTypes = await _repository.GetAllAsync();

            return consoleTypes
                .Select(ToResponse)
                .ToList();
        }

        public async Task<ConsoleTypeResponse?> GetByIdAsync(int id)
        {
            var consoleType = await _repository.GetByIdAsync(id);

            return consoleType == null
                ? null
                : ToResponse(consoleType);
        }

        public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
        {
            var normalizedName = name.Trim();
            var consoleTypes = await _repository.GetAllAsync();

            return consoleTypes.Any(consoleType =>
                (!excludeId.HasValue || consoleType.Id != excludeId.Value) &&
                string.Equals(
                    consoleType.Name.Trim(),
                    normalizedName,
                    StringComparison.OrdinalIgnoreCase));
        }

        public async Task<ConsoleTypeResponse> CreateAsync(ConsoleTypeRequest request)
        {
            var consoleType = new ConsoleType
            {
                Name = request.Name.Trim(),
                HourlyRate = request.HourlyRate,
                IsActive = request.IsActive
            };

            await _repository.AddAsync(consoleType);
            await _repository.SaveChangesAsync();
            _logger.LogInformation("Console type {ConsoleTypeId} ({Name}) created. HourlyRate: {HourlyRate}.", consoleType.Id, consoleType.Name, consoleType.HourlyRate);

            return ToResponse(consoleType);
        }

        public async Task<ConsoleTypeResponse?> UpdateAsync(
            int id,
            ConsoleTypeRequest request)
        {
            var consoleType = await _repository.GetByIdAsync(id);

            if (consoleType == null)
            {
                return null;
            }

            consoleType.Name = request.Name.Trim();
            consoleType.HourlyRate = request.HourlyRate;
            consoleType.IsActive = request.IsActive;

            _repository.Update(consoleType);
            await _repository.SaveChangesAsync();
            _logger.LogInformation("Console type {ConsoleTypeId} ({Name}) updated.", consoleType.Id, consoleType.Name);

            return ToResponse(consoleType);
        }

        public async Task<bool> DeactivateAsync(int id)
        {
            var consoleType = await _repository.GetByIdAsync(id);

            if (consoleType == null)
            {
                return false;
            }

            consoleType.IsActive = false;
            await _repository.SaveChangesAsync();
            _logger.LogInformation("Console type {ConsoleTypeId} ({Name}) deactivated.", consoleType.Id, consoleType.Name);

            return true;
        }

        private static ConsoleTypeResponse ToResponse(ConsoleType consoleType)
        {
            return new ConsoleTypeResponse
            {
                Id = consoleType.Id,
                Name = consoleType.Name,
                HourlyRate = consoleType.HourlyRate,
                IsActive = consoleType.IsActive
            };
        }
    }
}
