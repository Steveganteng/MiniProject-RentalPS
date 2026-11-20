using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPS_BE.DTOs;
using RentalPS_BE.Services;

namespace RentalPS_BE.Controllers
{
    [ApiController]
    [Route("api/rentals")]
    [Authorize(Roles = "Admin,Customer")]
    public class RentalsController : ControllerBase
    {
        private readonly IRentalService _rentalService;

        public RentalsController(IRentalService rentalService)
        {
            _rentalService = rentalService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var rentals = await _rentalService.GetAllAsync(
                userId,
                User.IsInRole("Admin"));

            return Ok(rentals);
        }

        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var rental = await _rentalService.GetByIdAsync(
                id,
                userId,
                User.IsInRole("Admin"));

            // Rental milik pengguna lain juga dikembalikan sebagai 404
            // agar keberadaan data pengguna lain tidak terbuka.
            return rental == null ? NotFound() : Ok(rental);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create([FromBody] RentalRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var created = await _rentalService.CreateAsync(userId, request);
            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);

        }

        [HttpPut("{id:int:min(1)}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Update(int id, [FromBody] RentalRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var updated = await _rentalService.UpdateAsync(id, userId, request);
            return updated == null ? NotFound() : Ok(updated);

        }

        // Rental tidak di-hard-delete. Pembatalan menyimpan histori transaksi.
        [HttpPost("{id:int:min(1)}/cancel")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Cancel(int id)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var cancelled = await _rentalService.CancelAsync(id, userId);
            return cancelled ? NoContent() : NotFound();

        }

        // Perubahan status operasional rental hanya boleh dilakukan oleh Admin.
        [HttpPut("{id:int:min(1)}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] RentalStatusRequest request)
        {

            var updated = await _rentalService.UpdateStatusAsync(id, request);
            return updated == null
                ? NotFound(new { message = "Rental tidak ditemukan." })
                : Ok(updated);

        }

        private bool TryGetCurrentUserId(out int userId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out userId) && userId > 0;
        }
    }
}
