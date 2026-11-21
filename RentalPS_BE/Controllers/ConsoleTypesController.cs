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

        public ConsoleTypesController(
            IConsoleTypeService consoleTypeService)
        {
            _consoleTypeService = consoleTypeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var consoleTypes =
                await _consoleTypeService.GetAllAsync();

            return Ok(consoleTypes);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var consoleType =
                await _consoleTypeService.GetByIdAsync(id);

            if (consoleType == null)
            {
                return NotFound();
            }

            return Ok(consoleType);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
            ConsoleTypeRequest request)
        {
            var created =
                await _consoleTypeService.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(
            int id,
            ConsoleTypeRequest request)
        {
            var updated =
                await _consoleTypeService.UpdateAsync(id, request);

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
            var deleted = await _consoleTypeService.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}