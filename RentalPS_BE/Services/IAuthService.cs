using RentalPS_BE.DTOs;

namespace RentalPS_BE.Services
{
    public interface IAuthService
    {
        Task<LoginResponse?> LoginAsync(LoginRequest request);
        Task<RegisterResponse?> RegisterAsync(RegisterRequest request);

    }
}