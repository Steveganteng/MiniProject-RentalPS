using Microsoft.AspNetCore.Authorization;
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(StickRequest request)
        {

            var created = await _stickService.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);

        }

        [HttpPut("{id:int:min(1)}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, StickRequest request)
        {

            var updated = await _stickService.UpdateAsync(id, request);

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
            var deactivated = await _stickService.DeactivateAsync(id);

            if (!deactivated)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
