using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPS_BE.DTOs;
using RentalPS_BE.Services;

namespace RentalPS_BE.Controllers
{
    [ApiController]
    [Route("api/rental-sticks")]
    [Authorize(Roles = "Admin,Customer")]
    public class RentalSticksController : ControllerBase
    {
        private readonly IRentalStickService _rentalStickService;

        public RentalSticksController(IRentalStickService rentalStickService)
        {
            _rentalStickService = rentalStickService;
        }

        // Admin dapat melihat seluruh detail stick.
        // Customer hanya dapat melihat detail stick dari rental miliknya.
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var rentalSticks = await _rentalStickService.GetAllAsync(
                userId,
                User.IsInRole("Admin"));

            return Ok(rentalSticks);
        }

        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var rentalStick = await _rentalStickService.GetByIdAsync(
                id,
                userId,
                User.IsInRole("Admin"));

            // Detail milik customer lain juga dikembalikan sebagai 404.
            return rentalStick == null ? NotFound() : Ok(rentalStick);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create([FromBody] RentalStickRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var created = await _rentalStickService.CreateAsync(userId, request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);

        }

        [HttpPut("{id:int:min(1)}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Update(int id, [FromBody] RentalStickRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var updated = await _rentalStickService.UpdateAsync(id, userId, request);
            return updated == null ? NotFound() : Ok(updated);

        }

        [HttpDelete("{id:int:min(1)}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var deleted = await _rentalStickService.DeleteAsync(id, userId);

            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();

        }

        private bool TryGetCurrentUserId(out int userId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out userId) && userId > 0;
        }
    }
}
