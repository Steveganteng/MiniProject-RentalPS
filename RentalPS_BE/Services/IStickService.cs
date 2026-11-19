using RentalPS_BE.DTOs;

namespace RentalPS_BE.Services
{
    public interface IStickService
    {
        Task<List<StickResponse>> GetAllAsync();
        Task<StickResponse?> GetByIdAsync(int id);
        Task<StickResponse> CreateAsync(StickRequest request);
        Task<StickResponse?> UpdateAsync(int id, StickRequest request);
        Task<bool> DeactivateAsync(int id);
    }
}
