using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPS_BE.DTOs;
using RentalPS_BE.Services;

namespace RentalPS_BE.Controllers
{
    [ApiController]
    [Route("api/rooms")]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _roomService;

        public RoomsController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var rooms = await _roomService.GetAllAsync();
            return Ok(rooms);
        }

        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetById(int id)
        {
            var room = await _roomService.GetByIdAsync(id);

            if (room == null)
            {
                return NotFound();
            }

            return Ok(room);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(RoomRequest request)
        {

            var created = await _roomService.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);

        }

        [HttpPut("{id:int:min(1)}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, RoomRequest request)
        {

            var updated = await _roomService.UpdateAsync(id, request);

            if (updated == null)
            {
                return NotFound();
            }

            return Ok(updated);

        }

        [HttpDelete("{id:int:min(1)}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var deactivated = await _roomService.DeactivateAsync(id);

            if (!deactivated)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
