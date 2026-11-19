using RentalPS_BE.DTOs;

namespace RentalPS_BE.Services
{
    public interface IRentalService
    {
        Task<List<RentalResponse>> GetAllAsync();
        Task<RentalResponse?> GetByIdAsync(int id);
        Task<RentalResponse> CreateAsync(RentalRequest request);
        Task<RentalResponse?> UpdateAsync(int id, RentalRequest request);
        Task<bool> CancelAsync(int id);
    }
}
