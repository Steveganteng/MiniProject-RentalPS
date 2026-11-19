using Microsoft.AspNetCore.Mvc;
using RentalPS_BE.DTOs;
using RentalPS_BE.Services;

namespace RentalPS_BE.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var payments = await _paymentService.GetAllAsync();
            return Ok(payments);
        }

        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetById(int id)
        {
            var payment = await _paymentService.GetByIdAsync(id);
            return payment == null ? NotFound() : Ok(payment);
        }

        [HttpGet("rental/{rentalId:int:min(1)}")]
        public async Task<IActionResult> GetByRentalId(int rentalId)
        {
            var payments = await _paymentService.GetByRentalIdAsync(rentalId);
            return payments == null ? NotFound() : Ok(payments);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PaymentRequest request)
        {
            try
            {
                var payment = await _paymentService.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int:min(1)}/status")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] PaymentStatusRequest request)
        {
            try
            {
                var payment = await _paymentService.UpdateStatusAsync(id, request);
                return payment == null ? NotFound() : Ok(payment);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
