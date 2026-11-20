using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPS_BE.DTOs;
using RentalPS_BE.Services;

namespace RentalPS_BE.Controllers
{
    [ApiController]
    [Route("api/payments")]
    [Authorize(Roles = "Admin,Customer")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // Admin dapat melihat seluruh pembayaran.
        // Customer hanya dapat melihat pembayaran dari rental miliknya.
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var payments = await _paymentService.GetAllAsync(
                userId,
                User.IsInRole("Admin"));

            return Ok(payments);
        }

        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var payment = await _paymentService.GetByIdAsync(
                id,
                userId,
                User.IsInRole("Admin"));

            // Pembayaran milik Customer lain juga dikembalikan sebagai 404.
            return payment == null ? NotFound() : Ok(payment);
        }

        [HttpGet("rental/{rentalId:int:min(1)}")]
        public async Task<IActionResult> GetByRentalId(int rentalId)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var payments = await _paymentService.GetByRentalIdAsync(
                rentalId,
                userId,
                User.IsInRole("Admin"));

            // Rental tidak ditemukan atau bukan milik Customer tersebut.
            return payments == null ? NotFound() : Ok(payments);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create([FromBody] PaymentRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var payment = await _paymentService.CreateAsync(userId, request);
            if (payment == null)
            {
                return NotFound(new
                {
                    message = "Rental tidak ditemukan atau bukan milik akun Anda."
                });
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = payment.Id },
                payment);

        }

        // Semua metode pembayaran (Cash, QRIS, Transfer) dikonfirmasi oleh Admin.
        [HttpPut("{id:int:min(1)}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] PaymentStatusRequest request)
        {

            var payment = await _paymentService.UpdateStatusAsync(id, request);
            return payment == null ? NotFound() : Ok(payment);

        }

        private bool TryGetCurrentUserId(out int userId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out userId) && userId > 0;
        }
    }
}
