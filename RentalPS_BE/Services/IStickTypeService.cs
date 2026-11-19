using RentalPS_BE.DTOs;

namespace RentalPS_BE.Services
{
    public interface IStickTypeService
    {
        Task<List<StickTypeResponse>> GetAllAsync();
        Task<StickTypeResponse?> GetByIdAsync(int id);
        Task<StickTypeResponse> CreateAsync(StickTypeRequest request);
        Task<StickTypeResponse?> UpdateAsync(int id, StickTypeRequest request);
        Task<bool> DeactivateAsync(int id);
    }
}
