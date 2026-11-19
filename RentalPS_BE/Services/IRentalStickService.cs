using RentalPS_BE.DTOs;

namespace RentalPS_BE.Services
{
    public interface IRentalStickService
    {
        Task<List<RentalStickResponse>> GetAllAsync();
        Task<RentalStickResponse?> GetByIdAsync(int id);
        Task<RentalStickResponse> CreateAsync(RentalStickRequest request);
        Task<RentalStickResponse?> UpdateAsync(int id, RentalStickRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
