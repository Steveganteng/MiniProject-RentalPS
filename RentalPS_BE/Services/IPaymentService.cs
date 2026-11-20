using RentalPS_BE.DTOs;

namespace RentalPS_BE.Services
{
    public interface IPaymentService
    {
        Task<List<PaymentResponse>> GetAllAsync(int userId, bool isAdmin);
        Task<PaymentResponse?> GetByIdAsync(int id, int userId, bool isAdmin);
        Task<List<PaymentResponse>?> GetByRentalIdAsync(int rentalId, int userId, bool isAdmin);
        Task<PaymentResponse?> CreateAsync(int userId, PaymentRequest request);
        Task<PaymentResponse?> UpdateStatusAsync(int id, PaymentStatusRequest request);
    }
}
