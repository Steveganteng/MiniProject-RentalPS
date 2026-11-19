using RentalPS_BE.DTOs;

namespace RentalPS_BE.Services
{
    public interface IRoomTypeService
    {
        Task<List<RoomTypeResponse>> GetAllAsync();
        Task<RoomTypeResponse> GetByIdAsync(int id);
        Task<RoomTypeResponse> CreateAsync(RoomTypeRequest request);
        Task<RoomTypeResponse?> UpdateAsync(int id, RoomTypeRequest request);
        Task<bool> DeactivateAsync(int id);
    }
}
