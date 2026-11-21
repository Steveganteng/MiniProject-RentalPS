using RentalPS_BE.DTOs;
using RentalPS_BE.Repositories;
using RentalPSMiniProject.Models;

namespace RentalPS_BE.Services
{
    public class ConsoleTypeService : IConsoleTypeService
    {
        private readonly IRepository<ConsoleType> _consoleTypeRepository;

        public ConsoleTypeService(
            IRepository<ConsoleType> consoleTypeRepository)
        {
            _consoleTypeRepository = consoleTypeRepository;
        }

        public async Task<List<ConsoleTypeResponse>> GetAllAsync()
        {
            var consoleTypes =
                await _consoleTypeRepository.GetAllAsync();

            return consoleTypes
                .Select(ToResponse)
                .ToList();
        }

        public async Task<ConsoleTypeResponse?> GetByIdAsync(int id)
        {
            var consoleType =
                await _consoleTypeRepository.GetByIdAsync(id);

            if (consoleType == null)
            {
                return null;
            }

            return ToResponse(consoleType);
        }

        public async Task<ConsoleTypeResponse> CreateAsync(
            ConsoleTypeRequest request)
        {
            var consoleType = new ConsoleType
            {
                Name = request.Name,
                HourlyRate = request.HourlyRate,
                IsActive = request.IsActive
            };

            await _consoleTypeRepository.AddAsync(consoleType);
            await _consoleTypeRepository.SaveChangesAsync();

            return ToResponse(consoleType);
        }

        public async Task<ConsoleTypeResponse?> UpdateAsync(
            int id,
            ConsoleTypeRequest request)
        {
            var consoleType =
                await _consoleTypeRepository.GetByIdAsync(id);

            if (consoleType == null)
            {
                return null;
            }

            consoleType.Name = request.Name;
            consoleType.HourlyRate = request.HourlyRate;
            consoleType.IsActive = request.IsActive;

            _consoleTypeRepository.Update(consoleType);
            await _consoleTypeRepository.SaveChangesAsync();

            return ToResponse(consoleType);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var consoleType =
                await _consoleTypeRepository.GetByIdAsync(id);

            if (consoleType == null)
            {
                return false;
            }

            // Soft delete karena ConsoleType memiliki field IsActive.
            consoleType.IsActive = false;

            _consoleTypeRepository.Update(consoleType);
            await _consoleTypeRepository.SaveChangesAsync();

            return true;
        }

        private static ConsoleTypeResponse ToResponse(
            ConsoleType consoleType)
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