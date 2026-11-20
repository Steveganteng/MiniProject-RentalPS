using RentalPS_BE.DTOs;

namespace RentalPS_BE.Services
{
    public interface IRentalService
    {
        Task<List<RentalResponse>> GetAllAsync(int userId, bool isAdmin);
        Task<RentalResponse?> GetByIdAsync(int id, int userId, bool isAdmin);
        Task<RentalResponse> CreateAsync(int userId, RentalRequest request);
        Task<RentalResponse?> UpdateAsync(int id, int userId, RentalRequest request);
        Task<bool> CancelAsync(int id, int userId);
        Task<RentalResponse?> UpdateStatusAsync(int id, RentalStatusRequest request);
    }
}
