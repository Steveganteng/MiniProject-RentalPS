using Microsoft.AspNetCore.Mvc;
using RentalPS_BE.DTOs;
using RentalPS_BE.Services;

namespace RentalPS_BE.Controllers
{
    [ApiController]
    [Route("api/rental-rooms")]
    public class RentalRoomsController : ControllerBase
    {
        private readonly IRentalRoomService _rentalRoomService;

        public RentalRoomsController(IRentalRoomService rentalRoomService)
        {
            _rentalRoomService = rentalRoomService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var rentalRooms = await _rentalRoomService.GetAllAsync();
            return Ok(rentalRooms);
        }

        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetById(int id)
        {
            var rentalRoom = await _rentalRoomService.GetByIdAsync(id);

            if (rentalRoom == null)
            {
                return NotFound();
            }

            return Ok(rentalRoom);
        }

        [HttpPost]
        public async Task<IActionResult> Create(RentalRoomRequest request)
        {
            try
            {
                var created = await _rentalRoomService.CreateAsync(request);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = created.Id },
                    created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int:min(1)}")]
        public async Task<IActionResult> Update(int id, RentalRoomRequest request)
        {
            try
            {
                var updated = await _rentalRoomService.UpdateAsync(id, request);

                if (updated == null)
                {
                    return NotFound();
                }

                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int:min(1)}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _rentalRoomService.DeleteAsync(id);

                if (!deleted)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
