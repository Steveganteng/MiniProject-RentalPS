using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPS_BE.DTOs;
using RentalPS_BE.Services;

namespace RentalPS_BE.Controllers
{
    [ApiController]
    [Route("api/room-types")]
    public class RoomTypesController : ControllerBase
    {
        private readonly IRoomTypeService _roomTypeService;

        public RoomTypesController(IRoomTypeService roomTypeService)
        {
            _roomTypeService = roomTypeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var roomTypes = await _roomTypeService.GetAllAsync();
            return Ok(roomTypes);
        }

        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetById(int id)
        {
            var roomType = await _roomTypeService.GetByIdAsync(id);

            if (roomType == null)
            {
                return NotFound();
            }

            return Ok(roomType);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(RoomTypeRequest request)
        {
            if (await _roomTypeService.NameExistsAsync(request.Name))
            {
                return Conflict(new
                {
                    message = "Nama room type sudah digunakan."
                });
            }

            var created = await _roomTypeService.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int:min(1)}")]
        public async Task<IActionResult> Update(int id, RoomTypeRequest request)
        {
            var existing = await _roomTypeService.GetByIdAsync(id);

            if (existing is null)
            {
                return NotFound();
            }

            if (await _roomTypeService.NameExistsAsync(request.Name, id))
            {
                return Conflict(new
                {
                    message = "Nama room type sudah digunakan."
                });
            }

            var updated = await _roomTypeService.UpdateAsync(id, request);

            if (updated == null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int:min(1)}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deactivated = await _roomTypeService.DeactivateAsync(id);

            if (!deactivated)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
