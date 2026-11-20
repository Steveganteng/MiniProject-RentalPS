using RentalPS_BE.DTOs;

namespace RentalPS_BE.Services
{
    public interface IRentalRoomService
    {
        Task<List<RentalRoomResponse>> GetAllAsync(int userId, bool isAdmin);
        Task<RentalRoomResponse?> GetByIdAsync(int id, int userId, bool isAdmin);
        Task<RentalRoomResponse> CreateAsync(int userId, RentalRoomRequest request);
        Task<RentalRoomResponse?> UpdateAsync(int id, int userId, RentalRoomRequest request);
        Task<bool> DeleteAsync(int id, int userId);
    }
}
