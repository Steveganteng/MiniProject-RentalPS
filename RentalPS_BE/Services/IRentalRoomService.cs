using RentalPS_BE.DTOs;

namespace RentalPS_BE.Services
{
    public interface IRentalRoomService
    {
        Task<List<RentalRoomResponse>> GetAllAsync();
        Task<RentalRoomResponse?> GetByIdAsync(int id);
        Task<RentalRoomResponse> CreateAsync(RentalRoomRequest request);
        Task<RentalRoomResponse?> UpdateAsync(int id, RentalRoomRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
