using RentalPS_BE.DTOs;

namespace RentalPS_BE.Services
{
    public interface IConsoleUnitService
    {
        Task<List<ConsoleUnitResponse>> GetAllAsync();
        Task<ConsoleUnitResponse?> GetByIdAsync(int id);
        Task<ConsoleUnitResponse> CreateAsync(ConsoleUnitRequest request);
        Task<ConsoleUnitResponse?> UpdateAsync(int id, ConsoleUnitRequest request);
        Task<bool> DeleteAsync(int id);
    }
}