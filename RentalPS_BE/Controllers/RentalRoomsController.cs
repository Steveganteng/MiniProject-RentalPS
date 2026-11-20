using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPS_BE.DTOs;
using RentalPS_BE.Services;

namespace RentalPS_BE.Controllers
{
    [ApiController]
    [Route("api/rental-rooms")]
    [Authorize(Roles = "Admin,Customer")]
    public class RentalRoomsController : ControllerBase
    {
        private readonly IRentalRoomService _rentalRoomService;

        public RentalRoomsController(IRentalRoomService rentalRoomService)
        {
            _rentalRoomService = rentalRoomService;
        }

        // Admin dapat melihat semua detail ruangan.
        // Customer hanya dapat melihat detail ruangan dari rental miliknya.
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var rentalRooms = await _rentalRoomService.GetAllAsync(
                userId,
                User.IsInRole("Admin"));

            return Ok(rentalRooms);
        }

        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var rentalRoom = await _rentalRoomService.GetByIdAsync(
                id,
                userId,
                User.IsInRole("Admin"));

            // Detail milik customer lain juga dikembalikan sebagai 404.
            return rentalRoom == null ? NotFound() : Ok(rentalRoom);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create([FromBody] RentalRoomRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var created = await _rentalRoomService.CreateAsync(userId, request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);

        }

        [HttpPut("{id:int:min(1)}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Update(int id, [FromBody] RentalRoomRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var updated = await _rentalRoomService.UpdateAsync(id, userId, request);
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

            var deleted = await _rentalRoomService.DeleteAsync(id, userId);
            return deleted ? NoContent() : NotFound();

        }

        private bool TryGetCurrentUserId(out int userId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out userId) && userId > 0;
        }
    }
}
