using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPS_BE.DTOs;
using RentalPS_BE.Services;

namespace RentalPS_BE.Controllers
{
    [Route("api/console-unit")]
    [ApiController]
    public class ConsoleUnitsController : ControllerBase
    {
        private readonly IConsoleUnitService _service;

        public ConsoleUnitsController(IConsoleUnitService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var units = await _service.GetAllAsync();
            return Ok(units);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var unit = await _service.GetByIdAsync(id);
            if (unit == null)
                return NotFound(new { message = "Console Unit tidak ditemukan" });

            return Ok(unit);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] ConsoleUnitRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var unit = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = unit.Id }, unit);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] ConsoleUnitRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var unit = await _service.UpdateAsync(id, request);
            if (unit == null)
                return NotFound(new { message = "Console Unit tidak ditemukan" });

            return Ok(unit);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound(new { message = "Console Unit tidak ditemukan" });

            return Ok(new { message = "Console Unit berhasil dihapus" });
        }
    }
}