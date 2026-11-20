using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalPS_BE.DTOs;
using RentalPS_BE.Services;

namespace RentalPS_BE.Controllers
{
    [ApiController]
    [Route("api/console-types")]
    public class ConsoleTypesController : ControllerBase
    {
        private readonly IConsoleTypeService _consoleTypeService;

        public ConsoleTypesController(IConsoleTypeService consoleTypeService)
        {
            _consoleTypeService = consoleTypeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var consoleTypes = await _consoleTypeService.GetAllAsync();
            return Ok(consoleTypes);
        }

        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetById(int id)
        {
            var consoleType = await _consoleTypeService.GetByIdAsync(id);

            if (consoleType == null)
            {
                return NotFound();
            }

            return Ok(consoleType);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(ConsoleTypeRequest request)
        {
            if (await _consoleTypeService.NameExistsAsync(request.Name))
            {
                return Conflict(new
                {
                    message = "Nama console type sudah digunakan."
                });
            }

            var created = await _consoleTypeService.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int:min(1)}")]
        public async Task<IActionResult> Update(int id, ConsoleTypeRequest request)
        {
            var existing = await _consoleTypeService.GetByIdAsync(id);

            if (existing is null)
            {
                return NotFound();
            }

            if (await _consoleTypeService.NameExistsAsync(request.Name, id))
            {
                return Conflict(new
                {
                    message = "Nama console type sudah digunakan."
                });
            }

            var updated = await _consoleTypeService.UpdateAsync(id, request);

            if (updated is null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int:min(1)}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _consoleTypeService.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}