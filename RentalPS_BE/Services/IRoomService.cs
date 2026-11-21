using RentalPS_BE.DTOs;

namespace RentalPS_BE.Services
{
    public interface IRoomService
    {
        Task<List<RoomResponse>> GetAllAsync();
        Task<RoomResponse?> GetByIdAsync(int id);
        Task<RoomResponse> CreateAsync(RoomRequest request);
        Task<RoomResponse?> UpdateAsync(int id, RoomRequest request);
        Task<bool> DeleteAsync(int id);
    }
}