using Microsoft.AspNetCore.Mvc;
using RentalPS_BE.DTOs;
using RentalPS_BE.Services;

namespace RentalPS_BE.Controllers
{
    [ApiController]
    [Route("api/rental-sticks")]
    public class RentalSticksController : ControllerBase
    {
        private readonly IRentalStickService _rentalStickService;

        public RentalSticksController(IRentalStickService rentalStickService)
        {
            _rentalStickService = rentalStickService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var rentalSticks = await _rentalStickService.GetAllAsync();
            return Ok(rentalSticks);
        }

        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetById(int id)
        {
            var rentalStick = await _rentalStickService.GetByIdAsync(id);

            if (rentalStick == null)
            {
                return NotFound();
            }

            return Ok(rentalStick);
        }

        [HttpPost]
        public async Task<IActionResult> Create(RentalStickRequest request)
        {
            try
            {
                var created = await _rentalStickService.CreateAsync(request);

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
        public async Task<IActionResult> Update(int id, RentalStickRequest request)
        {
            try
            {
                var updated = await _rentalStickService.UpdateAsync(id, request);

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
                var deleted = await _rentalStickService.DeleteAsync(id);

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
