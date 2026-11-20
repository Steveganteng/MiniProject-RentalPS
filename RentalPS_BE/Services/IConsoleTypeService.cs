using RentalPS_BE.DTOs;

namespace RentalPS_BE.Services
{
    public interface IConsoleTypeService
    {
        Task<List<ConsoleTypeResponse>> GetAllAsync();
        Task<ConsoleTypeResponse?> GetByIdAsync(int id);
        Task<ConsoleTypeResponse> CreateAsync(ConsoleTypeRequest request);
        Task<ConsoleTypeResponse?> UpdateAsync(int id, ConsoleTypeRequest request);
        Task<bool> DeleteAsync(int id);
    }
}