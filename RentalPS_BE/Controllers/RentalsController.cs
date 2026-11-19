using Microsoft.AspNetCore.Mvc;
using RentalPS_BE.DTOs;
using RentalPS_BE.Services;

namespace RentalPS_BE.Controllers
{
    [ApiController]
    [Route("api/rentals")]
    public class RentalsController : ControllerBase
    {
        private readonly IRentalService _rentalService;

        public RentalsController(IRentalService rentalService)
        {
            _rentalService = rentalService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var rentals = await _rentalService.GetAllAsync();
            return Ok(rentals);
        }

        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetById(int id)
        {
            var rental = await _rentalService.GetByIdAsync(id);
            return rental == null ? NotFound() : Ok(rental);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RentalRequest request)
        {
            try
            {
                var created = await _rentalService.CreateAsync(request);
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
        public async Task<IActionResult> Update(int id, [FromBody] RentalRequest request)
        {
            try
            {
                var updated = await _rentalService.UpdateAsync(id, request);
                return updated == null ? NotFound() : Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Rental tidak di-hard-delete. Pembatalan menyimpan histori transaksi.
        [HttpPost("{id:int:min(1)}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var cancelled = await _rentalService.CancelAsync(id);
                return cancelled ? NoContent() : NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
