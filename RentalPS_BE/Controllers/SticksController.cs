using Microsoft.AspNetCore.Mvc;
using RentalPS_BE.DTOs;
using RentalPS_BE.Services;

namespace RentalPS_BE.Controllers
{
    [ApiController]
    [Route("api/sticks")]
    public class SticksController : ControllerBase
    {
        private readonly IStickService _stickService;

        public SticksController(IStickService stickService)
        {
            _stickService = stickService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var sticks = await _stickService.GetAllAsync();
            return Ok(sticks);
        }

        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetById(int id)
        {
            var stick = await _stickService.GetByIdAsync(id);

            if (stick == null)
            {
                return NotFound();
            }

            return Ok(stick);
        }

        [HttpPost]
        public async Task<IActionResult> Create(StickRequest request)
        {
            try
            {
                var created = await _stickService.CreateAsync(request);

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
        public async Task<IActionResult> Update(int id, StickRequest request)
        {
            try
            {
                var updated = await _stickService.UpdateAsync(id, request);

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
            var deactivated = await _stickService.DeactivateAsync(id);

            if (!deactivated)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
