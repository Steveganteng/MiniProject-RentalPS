using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPS_BE.DTOs;
using RentalPS_BE.Services;

namespace RentalPS_BE.Controllers
{
    [ApiController]
    [Route("api/stick-types")]
    public class StickTypesController : ControllerBase
    {
        private readonly IStickTypeService _stickTypeService;

        public StickTypesController(IStickTypeService stickTypeService)
        {
            _stickTypeService = stickTypeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var stickTypes = await _stickTypeService.GetAllAsync();
            return Ok(stickTypes);
        }

        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetById(int id)
        {
            var stickType = await _stickTypeService.GetByIdAsync(id);

            if (stickType == null)
            {
                return NotFound();
            }

            return Ok(stickType);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(StickTypeRequest request)
        {
            var created = await _stickTypeService.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);
        }

        [HttpPut("{id:int:min(1)}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, StickTypeRequest request)
        {
            var updated = await _stickTypeService.UpdateAsync(id, request);

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
            var deactivated = await _stickTypeService.DeactivateAsync(id);

            if (!deactivated)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
