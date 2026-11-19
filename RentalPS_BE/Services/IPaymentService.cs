using RentalPS_BE.DTOs;

namespace RentalPS_BE.Services
{
    public interface IPaymentService
    {
        Task<List<PaymentResponse>> GetAllAsync();
        Task<PaymentResponse?> GetByIdAsync(int id);
        Task<List<PaymentResponse>?> GetByRentalIdAsync(int rentalId);
        Task<PaymentResponse> CreateAsync(PaymentRequest request);
        Task<PaymentResponse?> UpdateStatusAsync(int id, PaymentStatusRequest request);
    }
}
