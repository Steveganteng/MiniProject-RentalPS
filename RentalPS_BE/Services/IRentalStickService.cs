using RentalPS_BE.DTOs;

namespace RentalPS_BE.Services
{
    public interface IRentalStickService
    {
        Task<List<RentalStickResponse>> GetAllAsync(int userId, bool isAdmin);
        Task<RentalStickResponse?> GetByIdAsync(int id, int userId, bool isAdmin);
        Task<RentalStickResponse> CreateAsync(int userId, RentalStickRequest request);
        Task<RentalStickResponse?> UpdateAsync(int id, int userId, RentalStickRequest request);
        Task<bool> DeleteAsync(int id, int userId);
    }
}
