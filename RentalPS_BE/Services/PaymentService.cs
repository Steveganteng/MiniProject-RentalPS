using RentalPS_BE.DTOs;
using RentalPS_BE.Repositories;
using RentalPSMiniProject.Models;

namespace RentalPS_BE.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IRepository<Payment> _paymentRepository;
        private readonly IRepository<Rental> _rentalRepository;
        private readonly IRepository<RentalRoom> _rentalRoomRepository;
        private readonly IRepository<RentalStick> _rentalStickRepository;

        public PaymentService(
            IRepository<Payment> paymentRepository,
            IRepository<Rental> rentalRepository,
            IRepository<RentalRoom> rentalRoomRepository,
            IRepository<RentalStick> rentalStickRepository)
        {
            _paymentRepository = paymentRepository;
            _rentalRepository = rentalRepository;
            _rentalRoomRepository = rentalRoomRepository;
            _rentalStickRepository = rentalStickRepository;
        }

        public async Task<List<PaymentResponse>> GetAllAsync()
        {
            var payments = await _paymentRepository.GetAllAsync();

            return payments
                .OrderByDescending(p => p.Id)
                .Select(ToResponse)
                .ToList();
        }

        public async Task<PaymentResponse?> GetByIdAsync(int id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            return payment == null ? null : ToResponse(payment);
        }

        public async Task<List<PaymentResponse>?> GetByRentalIdAsync(int rentalId)
        {
            var rental = await _rentalRepository.GetByIdAsync(rentalId);
            if (rental == null)
            {
                return null;
            }

            var payments = await _paymentRepository.GetAllAsync();

            return payments
                .Where(p => p.RentalId == rentalId)
                .OrderByDescending(p => p.Id)
                .Select(ToResponse)
                .ToList();
        }

        public async Task<PaymentResponse> CreateAsync(PaymentRequest request)
        {
            if (request.RentalId <= 0)
            {
                throw new ArgumentException("RentalId harus lebih besar dari 0.");
            }

            if (request.Amount <= 0)
            {
                throw new ArgumentException("Amount harus lebih besar dari 0.");
            }

            if (!Enum.IsDefined(typeof(PaymentMethod), request.PaymentMethod))
            {
                throw new ArgumentException("PaymentMethod tidak valid.");
            }

            var rental = await _rentalRepository.GetByIdAsync(request.RentalId);
            if (rental == null)
            {
                throw new ArgumentException("RentalId tidak ditemukan.");
            }

            if (rental.StatusRental == StatusRental.Cancelled)
            {
                throw new ArgumentException(
                    "Pembayaran tidak dapat dibuat untuk rental yang sudah dibatalkan.");
            }

            if (rental.DurationHour <= 0 || rental.ConsoleRate < 0)
            {
                throw new ArgumentException("Data durasi atau tarif konsol rental belum valid.");
            }

            var rentalTotal = await CalculateRentalTotalAsync(rental);
            if (rentalTotal <= 0)
            {
                throw new ArgumentException(
                    "Total rental belum valid atau belum memiliki nilai pembayaran.");
            }

            var payments = await _paymentRepository.GetAllAsync();

            // Pending dihitung sebagai nominal yang sudah dialokasikan supaya
            // beberapa pembayaran pending tidak melampaui total tagihan.
            var reservedAmount = payments
                .Where(p => p.RentalId == rental.Id &&
                            (p.PaymentStatus == PaymentStatus.Pending ||
                             p.PaymentStatus == PaymentStatus.Success))
                .Sum(p => p.Amount);

            var outstandingAmount = rentalTotal - reservedAmount;
            if (outstandingAmount <= 0)
            {
                throw new ArgumentException(
                    "Rental sudah lunas atau tidak memiliki sisa tagihan.");
            }

            if (request.Amount > outstandingAmount)
            {
                throw new ArgumentException(
                    $"Jumlah pembayaran melebihi sisa tagihan. Sisa tagihan: {outstandingAmount:N2}.");
            }

            var payment = new Payment
            {
                RentalId = rental.Id,
                PaymentCode = GeneratePaymentCode(),
                Amount = request.Amount,
                PaymentMethod = request.PaymentMethod,
                PaymentStatus = PaymentStatus.Pending,
                PaidAt = null
            };

            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            return ToResponse(payment);
        }

        public async Task<PaymentResponse?> UpdateStatusAsync(
            int id,
            PaymentStatusRequest request)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment == null)
            {
                return null;
            }

            if (!Enum.IsDefined(typeof(PaymentStatus), request.PaymentStatus))
            {
                throw new ArgumentException("PaymentStatus tidak valid.");
            }

            if (payment.PaymentStatus != PaymentStatus.Pending)
            {
                throw new ArgumentException(
                    "Status pembayaran hanya dapat diubah ketika masih Pending.");
            }

            if (request.PaymentStatus == PaymentStatus.Pending)
            {
                throw new ArgumentException(
                    "Status tujuan harus Success atau Failed, bukan Pending.");
            }

            if (request.PaymentStatus == PaymentStatus.Success)
            {
                var rental = await _rentalRepository.GetByIdAsync(payment.RentalId);
                if (rental == null)
                {
                    throw new ArgumentException(
                        "Rental yang terhubung dengan pembayaran tidak ditemukan.");
                }

                if (rental.StatusRental == StatusRental.Cancelled)
                {
                    throw new ArgumentException(
                        "Pembayaran untuk rental yang sudah dibatalkan tidak dapat dikonfirmasi.");
                }

                var rentalTotal = await CalculateRentalTotalAsync(rental);
                var payments = await _paymentRepository.GetAllAsync();

                var otherReservedAmount = payments
                    .Where(p => p.RentalId == rental.Id &&
                                p.Id != payment.Id &&
                                (p.PaymentStatus == PaymentStatus.Pending ||
                                 p.PaymentStatus == PaymentStatus.Success))
                    .Sum(p => p.Amount);

                if (otherReservedAmount + payment.Amount > rentalTotal)
                {
                    throw new ArgumentException(
                        "Pembayaran tidak dapat dikonfirmasi karena jumlahnya melebihi total tagihan rental.");
                }

                payment.PaidAt = DateTime.UtcNow;
            }
            else
            {
                payment.PaidAt = null;
            }

            payment.PaymentStatus = request.PaymentStatus;
            _paymentRepository.Update(payment);
            await _paymentRepository.SaveChangesAsync();

            return ToResponse(payment);
        }

        private async Task<decimal> CalculateRentalTotalAsync(Rental rental)
        {
            var rentalRooms = await _rentalRoomRepository.GetAllAsync();
            var rentalSticks = await _rentalStickRepository.GetAllAsync();

            var roomTotal = rentalRooms
                .Where(rr => rr.RentalId == rental.Id)
                .Sum(rr => rr.SubTotal);

            var stickTotal = rentalSticks
                .Where(rs => rs.RentalId == rental.Id)
                .Sum(rs => rs.SubTotal);

            var consoleTotal = rental.ConsoleRate * rental.DurationHour;
            return consoleTotal + roomTotal + stickTotal;
        }

        private static string GeneratePaymentCode()
        {
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            return $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}-{suffix}";
        }

        private static PaymentResponse ToResponse(Payment payment)
        {
            return new PaymentResponse
            {
                Id = payment.Id,
                RentalId = payment.RentalId,
                PaymentCode = payment.PaymentCode,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                PaymentStatus = payment.PaymentStatus,
                PaidAt = payment.PaidAt
            };
        }
    }
}
